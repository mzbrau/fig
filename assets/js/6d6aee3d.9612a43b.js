"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[8873],{5680:(e,t,n)=>{n.d(t,{xA:()=>c,yg:()=>d});var r=n(6540);function i(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function a(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);t&&(r=r.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,r)}return n}function s(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?a(Object(n),!0).forEach((function(t){i(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):a(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function o(e,t){if(null==e)return{};var n,r,i=function(e,t){if(null==e)return{};var n,r,i={},a=Object.keys(e);for(r=0;r<a.length;r++)n=a[r],t.indexOf(n)>=0||(i[n]=e[n]);return i}(e,t);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);for(r=0;r<a.length;r++)n=a[r],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(i[n]=e[n])}return i}var l=r.createContext({}),u=function(e){var t=r.useContext(l),n=t;return e&&(n="function"==typeof e?e(t):s(s({},t),e)),n},c=function(e){var t=u(e.components);return r.createElement(l.Provider,{value:t},e.children)},g="mdxType",p={inlineCode:"code",wrapper:function(e){var t=e.children;return r.createElement(r.Fragment,{},t)}},m=r.forwardRef((function(e,t){var n=e.components,i=e.mdxType,a=e.originalType,l=e.parentName,c=o(e,["components","mdxType","originalType","parentName"]),g=u(n),m=i,d=g["".concat(l,".").concat(m)]||g[m]||p[m]||a;return n?r.createElement(d,s(s({ref:t},c),{},{components:n})):r.createElement(d,s({ref:t},c))}));function d(e,t){var n=arguments,i=t&&t.mdxType;if("string"==typeof e||i){var a=n.length,s=new Array(a);s[0]=m;var o={};for(var l in t)hasOwnProperty.call(t,l)&&(o[l]=t[l]);o.originalType=e,o[g]="string"==typeof e?e:i,s[1]=o;for(var u=2;u<a;u++)s[u]=n[u];return r.createElement.apply(null,s)}return r.createElement.apply(null,n)}m.displayName="MDXCreateElement"},5803:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>l,contentTitle:()=>s,default:()=>p,frontMatter:()=>a,metadata:()=>o,toc:()=>u});var r=n(8168),i=(n(6540),n(5680));const a={sidebar_position:7},s="Multi-Line Strings",o={unversionedId:"features/settings-management/multiline",id:"features/settings-management/multiline",title:"Multi-Line Strings",description:"Multi-line string settings are supported in Fig by adding an attribute to the string setting.",source:"@site/docs/features/settings-management/multiline.md",sourceDirName:"features/settings-management",slug:"/features/settings-management/multiline",permalink:"/docs/features/settings-management/multiline",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/features/settings-management/multiline.md",tags:[],version:"current",sidebarPosition:7,frontMatter:{sidebar_position:7},sidebar:"tutorialSidebar",previous:{title:"Ordering",permalink:"/docs/features/settings-management/ordering"},next:{title:"Valid Values (Dropdowns)",permalink:"/docs/features/settings-management/valid-values"}},l={},u=[{value:"Usage",id:"usage",level:2},{value:"Appearance",id:"appearance",level:2}],c={toc:u},g="wrapper";function p(e){let{components:t,...a}=e;return(0,i.yg)(g,(0,r.A)({},c,a,{components:t,mdxType:"MDXLayout"}),(0,i.yg)("h1",{id:"multi-line-strings"},"Multi-Line Strings"),(0,i.yg)("p",null,"Multi-line string settings are supported in Fig by adding an attribute to the string setting.\nThe number indicates how many lines will be shown in the editor."),(0,i.yg)("h2",{id:"usage"},"Usage"),(0,i.yg)("pre",null,(0,i.yg)("code",{parentName:"pre",className:"language-csharp"},'[Setting("Multi Line Setting")]\n[MultiLine(6)]\npublic string? MultiLineString { get; set; }\n')),(0,i.yg)("h2",{id:"appearance"},"Appearance"),(0,i.yg)("p",null,(0,i.yg)("img",{alt:"image-20220726221132075",src:n(780).A,width:"1832",height:"570"})))}p.isMDXComponent=!0},780:(e,t,n)=>{n.d(t,{A:()=>r});const r=n.p+"assets/images/image-20220726221132075-9564f360e93970728fa5dda53026d08a.png"}}]);