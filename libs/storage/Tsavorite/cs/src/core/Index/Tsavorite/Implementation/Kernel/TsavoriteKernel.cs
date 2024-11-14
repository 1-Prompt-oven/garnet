﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Tsavorite.core
{
    /// <summary>
    /// The core data structures of the core, used for dual Tsavorite operations
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public partial class TsavoriteKernel
    {
        // Unioned fields
        [FieldOffset(0)]
        internal HashTable hashTable;
        [FieldOffset(0)]
        internal HashBucketLockTable lockTable;

        [FieldOffset(HashTable.Size)]
        public LightEpoch Epoch;

        [FieldOffset(HashTable.Size + Constants.IntPtrSize)]
        internal ILogger Logger;

        const int KeyLockMaxRetryAttempts = 1000;
        internal const int DoNotCreateSlotAddress = -1;

        public TsavoriteKernel(long numBuckets, int sectorSize, ILogger logger = null)
        {
            if (!Utility.IsPowerOfTwo(numBuckets))
                throw new ArgumentException("Size {0} is not a power of 2");
            if (!Utility.Is32Bit(numBuckets))
                throw new ArgumentException("Size {0} is not 32-bit");

            this.Logger = logger;
            hashTable = new(numBuckets, sectorSize, logger);
            Epoch = new LightEpoch();
        }

        public bool EnsureEpochProtected()
        {
            if (Epoch.ThisInstanceProtected())
                return false;
            Epoch.Resume();
            return true;
        }

        /// <summary>
        /// Enter the kernel for a read operation on a single Tsavorite configuration or the first TsavoriteKV of a dual configuration, where epoch may or may not need to be acquired.
        /// </summary>
        /// <remarks>
        /// This does epoch entry and the hash bucket lookup and transient lock (which is the only hash bucket lock unless a RETRY is needed, in which case transient locks are released then reacquired by the TsavoriteKV instance).
        /// </remarks>
        /// <returns><see cref="Status.Found"/> if the tag was found, else <see cref="Status.NotFound"/></returns>
        public Status EnterForRead<TKernelSession, TKeyLocker, TEpochGuard>(ref TKernelSession kernelSession, long keyHash, ushort partitionId, out HashEntryInfo hei)
            where TKernelSession : IKernelSession
            where TKeyLocker : struct, IKeyLocker
            where TEpochGuard : struct, IEpochGuard<TKernelSession>
        {
            TEpochGuard.BeginUnsafe(ref kernelSession);

            // CheckHashTableGrowth(); TODO

            hei = new(keyHash, partitionId);
            if (!FindTag(ref hei))
            {
                TEpochGuard.EndUnsafe(ref kernelSession);
                return new(StatusCode.NotFound);
            }

            while (!TKeyLocker.TryLockTransientShared(this, ref hei))
            {
                // The kernelSession may be basic or dual; if dual, we want both stores to execute the Refresh ThreadStateMachineStep
                kernelSession.Refresh<TKeyLocker>(ref hei);
                _ = Thread.Yield();
            }

            // Between the time we found the tag and the time we locked the bucket the record in hei.entry may have been elided, so make sure we don't have a stale address in hei.entry.
            hei.SetToCurrent();

            return new(StatusCode.Found);
        }

        /// <summary>
        /// Enter the kernel for a read operation on a single Tsavorite configuration or the first TsavoriteKV of a dual configuration, where epoch has already been acquired.
        /// </summary>
        /// <remarks>
        /// This does epoch entry and the hash bucket lookup and transient lock (which is the only hash bucket lock unless a RETRY is needed, in which case transient locks are released then reacquired by the TsavoriteKV instance).
        /// </remarks>
        /// <returns><see cref="Status.Found"/> if the tag was found, else <see cref="Status.NotFound"/></returns>
        public Status UnsafeEnterForRead<TKernelSession, TKeyLocker>(ref TKernelSession kernelSession, long keyHash, ushort partitionId, out HashEntryInfo hei)
            where TKernelSession : IKernelSession
            where TKeyLocker : struct, IKeyLocker
        {
            hei = new(keyHash, partitionId);
            if (!FindTag(ref hei))
                return new(StatusCode.NotFound);

            while (!TKeyLocker.TryLockTransientShared(this, ref hei))
            {
                // The kernelSession may be basic or dual; if dual, we want both stores to execute the Refresh ThreadStateMachineStep
                kernelSession.Refresh<TKeyLocker>(ref hei);
                _ = Thread.Yield();
            }

            // Between the time we found the tag and the time we locked the bucket the record in hei.entry may have been elided, so make sure we don't have a stale address in hei.entry.
            hei.SetToCurrent();

            return new(StatusCode.Found);
        }

        /// <summary>
        /// Enter the kernel for a read operation on the second TsavoriteKV of a dual configuration.
        /// </summary>
        /// <remarks>
        /// This updates the hash bucket lookup. Epoch entry and hash bucket lock should have survived from EnterForReadDual1
        /// </remarks>
        /// <returns><see cref="Status.Found"/> if the tag was found, else <see cref="Status.NotFound"/></returns>
        public Status EnterForReadDual2(ushort partitionId, ref HashEntryInfo hei)
        {
            hei.Reset(partitionId);
            if (!FindTag(ref hei))
                return new(StatusCode.NotFound);
            Debug.Assert(hei.HasTransientSLock, "Expected transient HEI SLock after Reset and FindTag");
            Debug.Assert(lockTable.GetLockState(ref hei).IsLockedShared, "Expected bucket SLock after Reset and FindTag");
            return new(StatusCode.Found);
        }

        /// <summary>
        /// Find the slot for the tag if it is already in the hashtable.
        /// </summary>
        public bool FindTag(ref HashEntryInfo hei) => hashTable.FindTag(ref hei);

        /// <summary>
        /// Exits the kernel for this Read operation when the epoch may or may not be released
        /// </summary>
        /// <remarks>
        /// Releases the read lock on the hash bucket and releases the epoch.
        /// </remarks>
        public void ExitForRead<TKernelSession, TKeyLocker, TEpochGuard>(ref TKernelSession kernelSession, ref HashEntryInfo hei)
            where TKernelSession : IKernelSession
            where TKeyLocker : struct, IKeyLocker
            where TEpochGuard : struct, IEpochGuard<TKernelSession>
        {
            TKeyLocker.UnlockTransientShared(this, ref hei);
            if (Epoch.ThisInstanceProtected())
                TEpochGuard.EndUnsafe(ref kernelSession);
        }

        /// <summary>
        /// Exits the kernel for this Read operation when the epoch is not to be released
        /// </summary>
        /// <remarks>
        /// Releases the read lock on the hash bucket and releases the epoch.
        /// </remarks>
        public void UnsafeExitForRead<TKernelSession, TKeyLocker>(ref TKernelSession kernelSession, ref HashEntryInfo hei)
            where TKernelSession : IKernelSession
            where TKeyLocker : struct, IKeyLocker
        {
            TKeyLocker.UnlockTransientShared(this, ref hei);
        }

        /// <summary>
        /// Enter the kernel for an update operation on a single Tsavorite configuration or on the first TsavoriteKV of a dual configuration, where the epoch may or may not need to be acquired.
        /// </summary>
        /// <remarks>
        /// This does epoch entry and the hash bucket lookup and transient lock (which is the only hash bucket lock unless a RETRY is needed, in which case transient locks are released then reacquired by the TsavoriteKV instance).
        /// <para/>
        /// This is a strict update operation from the HashBucket perspective; if the tag is not found, we do not create it. It is used for operations like Delete or Expire. Operations that do in-place updates or RCU will know
        /// which store they are operating on and will call a different method.
        /// </remarks>
        /// <returns><see cref="Status.Found"/> if the tag was found, else <see cref="Status.NotFound"/></returns>
        public Status EnterForUpdate<TKernelSession, TKeyLocker, TEpochGuard>(ref TKernelSession kernelSession, long keyHash, ushort partitionId, long createIfNotFoundBeginAddress, out HashEntryInfo hei)
            where TKernelSession : IKernelSession
            where TKeyLocker : struct, IKeyLocker
            where TEpochGuard : struct, IEpochGuard<TKernelSession>
        {
            TEpochGuard.BeginUnsafe(ref kernelSession);

            // CheckHashTableGrowth(); TODO

            hei = new(keyHash, partitionId);

            // We may do FindTag rather than FindOrCreateTag here because we don't want to create the slot if the tag is not found.
            if (createIfNotFoundBeginAddress != DoNotCreateSlotAddress)
                FindOrCreateTag(ref hei, createIfNotFoundBeginAddress);
            else if (!FindTag(ref hei))
            {
                TEpochGuard.EndUnsafe(ref kernelSession);
                return new(StatusCode.NotFound);
            }

            while (!TKeyLocker.TryLockTransientExclusive(this, ref hei))
            {
                // The kernelSession may be basic or dual; if dual, we want both stores to execute the Refresh ThreadStateMachineStep
                kernelSession.Refresh<TKeyLocker>(ref hei);
                _ = Thread.Yield();
            }

            // Between the time we found the tag and the time we locked the bucket the record in hei.entry may have been elided, so make sure we don't have a stale address in hei.entry.
            hei.SetToCurrent();

            return new(StatusCode.Found);
        }

        /// <summary>
        /// Find the slot for the tag if it is already in the hashtable.
        /// </summary>
        public void FindOrCreateTag(ref HashEntryInfo hei, long createIfNotFoundBeginAddress) => hashTable.FindOrCreateTag(ref hei, createIfNotFoundBeginAddress);

        /// <summary>
        /// Enter the kernel for an update operation on a single Tsavorite configuration or on the first TsavoriteKV of a dual configuration, where the epoch has already been acquired.
        /// </summary>
        /// <remarks>
        /// This does epoch entry and the hash bucket lookup and transient lock (which is the only hash bucket lock unless a RETRY is needed, in which case transient locks are released then reacquired by the TsavoriteKV instance).
        /// <para/>
        /// This is a strict update operation from the HashBucket perspective; if the tag is not found, we do not create it. It is used for operations like Delete or Expire. Operations that do in-place updates or RCU will know
        /// which store they are operating on and will call a different method.
        /// </remarks>
        /// <returns><see cref="Status.Found"/> if the tag was found, else <see cref="Status.NotFound"/></returns>
        public Status UnsafeEnterForUpdate<TKernelSession, TKeyLocker>(ref TKernelSession kernelSession, long keyHash, ushort partitionId, long createIfNotFoundBeginAddress, out HashEntryInfo hei)
            where TKernelSession : IKernelSession
            where TKeyLocker : struct, IKeyLocker
        {
            hei = new(keyHash, partitionId);

            // We may do FindTag rather than FindOrCreateTag here because we don't want to create the slot if the tag is not found.
            if (createIfNotFoundBeginAddress != DoNotCreateSlotAddress)
                FindOrCreateTag(ref hei, createIfNotFoundBeginAddress);
            else if (!FindTag(ref hei))
                return new(StatusCode.NotFound);

            while (!TKeyLocker.TryLockTransientExclusive(this, ref hei))
            {
                // The kernelSession may be basic or dual; if dual, we want both stores to execute the Refresh ThreadStateMachineStep
                kernelSession.Refresh<TKeyLocker>(ref hei);
                _ = Thread.Yield();
            }

            // Between the time we found the tag and the time we locked the bucket the record in hei.entry may have been elided, so make sure we don't have a stale address in hei.entry.
            hei.SetToCurrent();

            return new(StatusCode.Found);
        }

        /// <summary>
        /// Enter the kernel for an update operation on the second TsavoriteKV of a dual configuration.
        /// </summary>
        /// <remarks>
        /// This is a strict update operation from the HashBucket perspective; if the tag is not found, we do not create it. It is used for operations like Delete or Expire. Operations that do in-place updates or RCU will know
        /// which store they are operating on and will call a different method.
        /// </remarks>
        public Status EnterForUpdateDual2(ushort partitionId, ref HashEntryInfo hei, long createIfNotFoundBeginAddress)
        {
            hei.Reset(partitionId);

            // We always want to create the tag here if it is not found
            FindOrCreateTag(ref hei, createIfNotFoundBeginAddress);
            Debug.Assert(hei.HasTransientXLock, "Expected transient XLock after Reset and FindTag");
            return new(StatusCode.Found);
        }

        /// <summary>
        /// Exits the kernel for this Update operation when epoch may or may not be released
        /// </summary>
        /// <remarks>
        /// Releases the read lock on the hash bucket and releases the epoch.
        /// </remarks>
        public void ExitForUpdate<TKernelSession, TKeyLocker, TEpochGuard>(ref TKernelSession kernelSession, ref HashEntryInfo hei)
            where TKernelSession : IKernelSession
            where TKeyLocker : struct, IKeyLocker
            where TEpochGuard : struct, IEpochGuard<TKernelSession>
        {
            TKeyLocker.UnlockTransientExclusive(this, ref hei);
            if (Epoch.ThisInstanceProtected())
                TEpochGuard.EndUnsafe(ref kernelSession);
        }

        /// <summary>
        /// Exits the kernel for this Update operation when epoch is not to be released
        /// </summary>
        /// <remarks>
        /// Releases the read lock on the hash bucket and releases the epoch.
        /// </remarks>
        public void UnsafeExitForUpdate<TKernelSession, TKeyLocker>(ref TKernelSession kernelSession, ref HashEntryInfo hei)
            where TKernelSession : IKernelSession
            where TKeyLocker : struct, IKeyLocker
        {
            TKeyLocker.UnlockTransientExclusive(this, ref hei);
        }

        internal void Dispose()
        {
            hashTable.Dispose();
            Epoch.Dispose();
        }
    }
}
