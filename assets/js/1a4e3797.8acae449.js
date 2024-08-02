"use strict";(self.webpackChunkwebsite=self.webpackChunkwebsite||[]).push([[2138],{5846:(e,t,r)=>{r.d(t,{W:()=>u});var s=r(6540),a=r(4586);const n=["zero","one","two","few","many","other"];function c(e){return n.filter((t=>e.includes(t)))}const l={locale:"en",pluralForms:c(["one","other"]),select:e=>1===e?"one":"other"};function o(){const{i18n:{currentLocale:e}}=(0,a.default)();return(0,s.useMemo)((()=>{try{return function(e){const t=new Intl.PluralRules(e);return{locale:e,pluralForms:c(t.resolvedOptions().pluralCategories),select:e=>t.select(e)}}(e)}catch(t){return console.error(`Failed to use Intl.PluralRules for locale "${e}".\nDocusaurus will fallback to the default (English) implementation.\nError: ${t.message}\n`),l}}),[e])}function u(){const e=o();return{selectMessage:(t,r)=>function(e,t,r){const s=e.split("|");if(1===s.length)return s[0];s.length>r.pluralForms.length&&console.error(`For locale=${r.locale}, a maximum of ${r.pluralForms.length} plural forms are expected (${r.pluralForms.join(",")}), but the message contains ${s.length}: ${e}`);const a=r.select(t),n=r.pluralForms.indexOf(a);return s[Math.min(n,s.length-1)]}(r,t,e)}}},1283:(e,t,r)=>{r.r(t),r.d(t,{default:()=>b});var s=r(6540),a=r(4586),n=r(2415),c=r(5260),l=r(8774),o=r(1312),u=r(5846),h=r(5391),i=r(6347),m=r(2303),d=r(1088);const p=function(){const e=(0,m.A)(),t=(0,i.W6)(),r=(0,i.zy)(),{siteConfig:{baseUrl:s}}=(0,a.default)(),n=e?new URLSearchParams(r.search):null,c=n?.get("q")||"",l=n?.get("ctx")||"",o=n?.get("version")||"",u=e=>{const t=new URLSearchParams(r.search);return e?t.set("q",e):t.delete("q"),t};return{searchValue:c,searchContext:l&&Array.isArray(d.Hg)&&d.Hg.some((e=>"string"==typeof e?e===l:e.path===l))?l:"",searchVersion:o,updateSearchPath:e=>{const r=u(e);t.replace({search:r.toString()})},updateSearchContext:e=>{const s=new URLSearchParams(r.search);s.set("ctx",e),t.replace({search:s.toString()})},generateSearchPageLink:e=>{const t=u(e);return`${s}search?${t.toString()}`}}};var g=r(5891),f=r(2384),x=r(9913),y=r(6841),C=r(3810),S=r(7674),j=r(2849),w=r(4471);const I={searchContextInput:"searchContextInput_mXoe",searchQueryInput:"searchQueryInput_CFBF",searchResultItem:"searchResultItem_U687",searchResultItemPath:"searchResultItemPath_uIbk",searchResultItemSummary:"searchResultItemSummary_oZHr",searchQueryColumn:"searchQueryColumn_q7nx",searchContextColumn:"searchContextColumn_oWAF"};var v=r(3385),A=r(4848);function R(){const{siteConfig:{baseUrl:e},i18n:{currentLocale:t}}=(0,a.default)(),{selectMessage:r}=(0,u.W)(),{searchValue:n,searchContext:l,searchVersion:i,updateSearchPath:m,updateSearchContext:x}=p(),[y,C]=(0,s.useState)(n),[S,w]=(0,s.useState)(),[R,b]=(0,s.useState)(),F=`${e}${i}`,T=(0,s.useMemo)((()=>y?(0,o.T)({id:"theme.SearchPage.existingResultsTitle",message:'Search results for "{query}"',description:"The search page title for non-empty query"},{query:y}):(0,o.T)({id:"theme.SearchPage.emptyResultsTitle",message:"Search the documentation",description:"The search page title for empty query"})),[y]);(0,s.useEffect)((()=>{m(y),S&&(y?S(y,(e=>{b(e)})):b(void 0))}),[y,S]);const _=(0,s.useCallback)((e=>{C(e.target.value)}),[]);return(0,s.useEffect)((()=>{n&&n!==y&&C(n)}),[n]),(0,s.useEffect)((()=>{!async function(){const{wrappedIndexes:e,zhDictionary:t}=!Array.isArray(d.Hg)||l||d.dz?await(0,g.Z)(F,l):{wrappedIndexes:[],zhDictionary:[]};w((()=>(0,f.m)(e,t,100)))}()}),[l,F]),(0,A.jsxs)(s.Fragment,{children:[(0,A.jsxs)(c.A,{children:[(0,A.jsx)("meta",{property:"robots",content:"noindex, follow"}),(0,A.jsx)("title",{children:T})]}),(0,A.jsxs)("div",{className:"container margin-vert--lg",children:[(0,A.jsx)("h1",{children:T}),(0,A.jsxs)("div",{className:"row",children:[(0,A.jsx)("div",{className:(0,h.A)("col",{[I.searchQueryColumn]:Array.isArray(d.Hg),"col--9":Array.isArray(d.Hg),"col--12":!Array.isArray(d.Hg)}),children:(0,A.jsx)("input",{type:"search",name:"q",className:I.searchQueryInput,"aria-label":"Search",onChange:_,value:y,autoComplete:"off",autoFocus:!0})}),Array.isArray(d.Hg)?(0,A.jsx)("div",{className:(0,h.A)("col","col--3","padding-left--none",I.searchContextColumn),children:(0,A.jsxs)("select",{name:"search-context",className:I.searchContextInput,id:"context-selector",value:l,onChange:e=>x(e.target.value),children:[d.dz&&(0,A.jsx)("option",{value:"",children:(0,o.T)({id:"theme.SearchPage.searchContext.everywhere",message:"Everywhere"})}),d.Hg.map((e=>{const{label:r,path:s}=(0,v.p)(e,t);return(0,A.jsx)("option",{value:s,children:r},s)}))]})}):null]}),!S&&y&&(0,A.jsx)("div",{children:(0,A.jsx)(j.A,{})}),R&&(R.length>0?(0,A.jsx)("p",{children:r(R.length,(0,o.T)({id:"theme.SearchPage.documentsFound.plurals",message:"1 document found|{count} documents found",description:'Pluralized label for "{count} documents found". Use as much plural forms (separated by "|") as your language support (see https://www.unicode.org/cldr/cldr-aux/charts/34/supplemental/language_plural_rules.html)'},{count:R.length}))}):(0,A.jsx)("p",{children:(0,o.T)({id:"theme.SearchPage.noResultsText",message:"No documents were found",description:"The paragraph for empty search result"})})),(0,A.jsx)("section",{children:R&&R.map((e=>(0,A.jsx)(P,{searchResult:e},e.document.i)))})]})]})}function P(e){let{searchResult:{document:t,type:r,page:s,tokens:a,metadata:n}}=e;const c=r===x.i.Title,o=r===x.i.Keywords,u=r===x.i.Description,h=u||o,i=c||h,m=r===x.i.Content,p=(c?t.b:s.b).slice(),g=m||h?t.s:t.t;i||p.push(s.t);let f="";if(d.CU&&a.length>0){const e=new URLSearchParams;for(const t of a)e.append("_highlight",t);f=`?${e.toString()}`}return(0,A.jsxs)("article",{className:I.searchResultItem,children:[(0,A.jsx)("h2",{children:(0,A.jsx)(l.A,{to:t.u+f+(t.h||""),dangerouslySetInnerHTML:{__html:m||h?(0,y.Z)(g,a):(0,C.C)(g,(0,S.g)(n,"t"),a,100)}})}),p.length>0&&(0,A.jsx)("p",{className:I.searchResultItemPath,children:(0,w.$)(p)}),(m||u)&&(0,A.jsx)("p",{className:I.searchResultItemSummary,dangerouslySetInnerHTML:{__html:(0,C.C)(t.t,(0,S.g)(n,"t"),a,100)}})]})}const b=function(){return(0,A.jsx)(n.A,{children:(0,A.jsx)(R,{})})}}}]);