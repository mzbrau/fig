"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[2403],{5680:(e,t,n)=>{n.d(t,{xA:()=>d,yg:()=>m});var a=n(6540);function r(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function i(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);t&&(a=a.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,a)}return n}function s(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?i(Object(n),!0).forEach((function(t){r(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):i(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function o(e,t){if(null==e)return{};var n,a,r=function(e,t){if(null==e)return{};var n,a,r={},i=Object.keys(e);for(a=0;a<i.length;a++)n=i[a],t.indexOf(n)>=0||(r[n]=e[n]);return r}(e,t);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);for(a=0;a<i.length;a++)n=i[a],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(r[n]=e[n])}return r}var c=a.createContext({}),l=function(e){var t=a.useContext(c),n=t;return e&&(n="function"==typeof e?e(t):s(s({},t),e)),n},d=function(e){var t=l(e.components);return a.createElement(c.Provider,{value:t},e.children)},g="mdxType",u={inlineCode:"code",wrapper:function(e){var t=e.children;return a.createElement(a.Fragment,{},t)}},p=a.forwardRef((function(e,t){var n=e.components,r=e.mdxType,i=e.originalType,c=e.parentName,d=o(e,["components","mdxType","originalType","parentName"]),g=l(n),p=r,m=g["".concat(c,".").concat(p)]||g[p]||u[p]||i;return n?a.createElement(m,s(s({ref:t},d),{},{components:n})):a.createElement(m,s({ref:t},d))}));function m(e,t){var n=arguments,r=t&&t.mdxType;if("string"==typeof e||r){var i=n.length,s=new Array(i);s[0]=p;var o={};for(var c in t)hasOwnProperty.call(t,c)&&(o[c]=t[c]);o.originalType=e,o[g]="string"==typeof e?e:r,s[1]=o;for(var l=2;l<i;l++)s[l]=n[l];return a.createElement.apply(null,s)}return a.createElement.apply(null,n)}p.displayName="MDXCreateElement"},9744:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>c,contentTitle:()=>s,default:()=>u,frontMatter:()=>i,metadata:()=>o,toc:()=>l});var a=n(8168),r=(n(6540),n(5680));const i={sidebar_position:5},s="Advanced Settings",o={unversionedId:"features/settings-management/advanced-settings",id:"features/settings-management/advanced-settings",title:"Advanced Settings",description:"An advanced setting is one that has a reasonable default value that would not need to be changed in a normal deployment. As a result, Fig will hide this setting by default to allow those configuring the application to focus on the settings that do need to be changed. An example might be a timeout value. In most cases, the developer will have chosen a reasonable default but the setting is still included incase it needs to be changed for debugging for when the application is deployed in a specific environment.",source:"@site/docs/features/settings-management/advanced-settings.md",sourceDirName:"features/settings-management",slug:"/features/settings-management/advanced-settings",permalink:"/docs/features/settings-management/advanced-settings",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/features/settings-management/advanced-settings.md",tags:[],version:"current",sidebarPosition:5,frontMatter:{sidebar_position:5},sidebar:"tutorialSidebar",previous:{title:"Secret Settings",permalink:"/docs/features/settings-management/secret-settings"},next:{title:"Ordering",permalink:"/docs/features/settings-management/ordering"}},c={},l=[{value:"Usage",id:"usage",level:2},{value:"Appearance",id:"appearance",level:2}],d={toc:l},g="wrapper";function u(e){let{components:t,...i}=e;return(0,r.yg)(g,(0,a.A)({},d,i,{components:t,mdxType:"MDXLayout"}),(0,r.yg)("h1",{id:"advanced-settings"},"Advanced Settings"),(0,r.yg)("p",null,"An advanced setting is one that has a reasonable default value that would not need to be changed in a normal deployment. As a result, Fig will hide this setting by default to allow those configuring the application to focus on the settings that do need to be changed. An example might be a timeout value. In most cases, the developer will have chosen a reasonable default but the setting is still included incase it needs to be changed for debugging for when the application is deployed in a specific environment."),(0,r.yg)("h2",{id:"usage"},"Usage"),(0,r.yg)("pre",null,(0,r.yg)("code",{parentName:"pre",className:"language-csharp"},'[Advanced]\n[Setting("Long Setting", 99)]\npublic long LongSetting { get; set; }\n')),(0,r.yg)("h2",{id:"appearance"},"Appearance"),(0,r.yg)("p",null,(0,r.yg)("img",{alt:"advanced-settings",src:n(7591).A,width:"2878",height:"866"})))}u.isMDXComponent=!0},7591:(e,t,n)=>{n.d(t,{A:()=>a});const a=n.p+"assets/images/advanced-setting-4058db938ed05bafa565fd2e18668560.png"}}]);