"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[4844],{3905:(e,t,n)=>{n.d(t,{Zo:()=>f,kt:()=>g});var i=n(7294);function r(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function a(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);t&&(i=i.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,i)}return n}function o(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?a(Object(n),!0).forEach((function(t){r(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):a(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function l(e,t){if(null==e)return{};var n,i,r=function(e,t){if(null==e)return{};var n,i,r={},a=Object.keys(e);for(i=0;i<a.length;i++)n=a[i],t.indexOf(n)>=0||(r[n]=e[n]);return r}(e,t);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);for(i=0;i<a.length;i++)n=a[i],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(r[n]=e[n])}return r}var s=i.createContext({}),c=function(e){var t=i.useContext(s),n=t;return e&&(n="function"==typeof e?e(t):o(o({},t),e)),n},f=function(e){var t=c(e.components);return i.createElement(s.Provider,{value:t},e.children)},u="mdxType",p={inlineCode:"code",wrapper:function(e){var t=e.children;return i.createElement(i.Fragment,{},t)}},d=i.forwardRef((function(e,t){var n=e.components,r=e.mdxType,a=e.originalType,s=e.parentName,f=l(e,["components","mdxType","originalType","parentName"]),u=c(n),d=r,g=u["".concat(s,".").concat(d)]||u[d]||p[d]||a;return n?i.createElement(g,o(o({ref:t},f),{},{components:n})):i.createElement(g,o({ref:t},f))}));function g(e,t){var n=arguments,r=t&&t.mdxType;if("string"==typeof e||r){var a=n.length,o=new Array(a);o[0]=d;var l={};for(var s in t)hasOwnProperty.call(t,s)&&(l[s]=t[s]);l.originalType=e,l[u]="string"==typeof e?e:r,o[1]=l;for(var c=2;c<a;c++)o[c]=n[c];return i.createElement.apply(null,o)}return i.createElement.apply(null,n)}d.displayName="MDXCreateElement"},8318:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>s,contentTitle:()=>o,default:()=>p,frontMatter:()=>a,metadata:()=>l,toc:()=>c});var i=n(7462),r=(n(7294),n(3905));const a={sidebar_position:12},o="Offline Settings",l={unversionedId:"features/offline-settings",id:"features/offline-settings",title:"Offline Settings",description:"By default, clients using the Fig.Client nuget package will support offline settings. This is a fallback mechanism for when the Fig.API is offline. If the client application starts and it is unable to contact the API, it will attempt to load the last values that it got from the API and run with those. It will continue to attempt to contact the API and will update the settings once successfully reconnected.",source:"@site/docs/features/offline-settings.md",sourceDirName:"features",slug:"/features/offline-settings",permalink:"/docs/features/offline-settings",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/features/offline-settings.md",tags:[],version:"current",sidebarPosition:12,frontMatter:{sidebar_position:12},sidebar:"tutorialSidebar",previous:{title:"API Management",permalink:"/docs/features/api-management"},next:{title:"Live Reload",permalink:"/docs/features/live-reload"}},s={},c=[],f={toc:c},u="wrapper";function p(e){let{components:t,...n}=e;return(0,r.kt)(u,(0,i.Z)({},f,n,{components:t,mdxType:"MDXLayout"}),(0,r.kt)("h1",{id:"offline-settings"},"Offline Settings"),(0,r.kt)("p",null,"By default, clients using the ",(0,r.kt)("inlineCode",{parentName:"p"},"Fig.Client")," nuget package will support offline settings. This is a fallback mechanism for when the Fig.API is offline. If the client application starts and it is unable to contact the API, it will attempt to load the last values that it got from the API and run with those. It will continue to attempt to contact the API and will update the settings once successfully reconnected."),(0,r.kt)("p",null,"The offline settings cache is stored as an encrypted binary file in a Fig directory in the local application data directory of the host machine. The client secret is used as the encryption / decryption key."),(0,r.kt)("p",null,"Offline settings can be disabled in the client configuration:"),(0,r.kt)("pre",null,(0,r.kt)("code",{parentName:"pre",className:"language-csharp"},'var configuration = new ConfigurationBuilder()\n    .AddFig<Settings>(o =>\n    {\n        o.ClientName = "AspNetApi";\n        o.AllowOfflineSettings = false;\n    }).Build();\n')))}p.isMDXComponent=!0}}]);