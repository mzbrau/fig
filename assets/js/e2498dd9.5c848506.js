"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[726],{5680:(e,t,n)=>{n.d(t,{xA:()=>p,yg:()=>y});var r=n(6540);function i(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function o(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);t&&(r=r.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,r)}return n}function a(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?o(Object(n),!0).forEach((function(t){i(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):o(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function c(e,t){if(null==e)return{};var n,r,i=function(e,t){if(null==e)return{};var n,r,i={},o=Object.keys(e);for(r=0;r<o.length;r++)n=o[r],t.indexOf(n)>=0||(i[n]=e[n]);return i}(e,t);if(Object.getOwnPropertySymbols){var o=Object.getOwnPropertySymbols(e);for(r=0;r<o.length;r++)n=o[r],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(i[n]=e[n])}return i}var s=r.createContext({}),l=function(e){var t=r.useContext(s),n=t;return e&&(n="function"==typeof e?e(t):a(a({},t),e)),n},p=function(e){var t=l(e.components);return r.createElement(s.Provider,{value:t},e.children)},u="mdxType",d={inlineCode:"code",wrapper:function(e){var t=e.children;return r.createElement(r.Fragment,{},t)}},g=r.forwardRef((function(e,t){var n=e.components,i=e.mdxType,o=e.originalType,s=e.parentName,p=c(e,["components","mdxType","originalType","parentName"]),u=l(n),g=i,y=u["".concat(s,".").concat(g)]||u[g]||d[g]||o;return n?r.createElement(y,a(a({ref:t},p),{},{components:n})):r.createElement(y,a({ref:t},p))}));function y(e,t){var n=arguments,i=t&&t.mdxType;if("string"==typeof e||i){var o=n.length,a=new Array(o);a[0]=g;var c={};for(var s in t)hasOwnProperty.call(t,s)&&(c[s]=t[s]);c.originalType=e,c[u]="string"==typeof e?e:i,a[1]=c;for(var l=2;l<o;l++)a[l]=n[l];return r.createElement.apply(null,a)}return r.createElement.apply(null,n)}g.displayName="MDXCreateElement"},4882:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>s,contentTitle:()=>a,default:()=>d,frontMatter:()=>o,metadata:()=>c,toc:()=>l});var r=n(8168),i=(n(6540),n(5680));const o={sidebar_position:3},a="Encrypting Client Secrets",c={unversionedId:"guides/encrypting-secrets-dpapi",id:"guides/encrypting-secrets-dpapi",title:"Encrypting Client Secrets",description:"When installed on Windows, Fig requires client secrets to be encrypted in DPAPI.",source:"@site/docs/guides/encrypting-secrets-dpapi.md",sourceDirName:"guides",slug:"/guides/encrypting-secrets-dpapi",permalink:"/docs/guides/encrypting-secrets-dpapi",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/guides/encrypting-secrets-dpapi.md",tags:[],version:"current",sidebarPosition:3,frontMatter:{sidebar_position:3},sidebar:"tutorialSidebar",previous:{title:"Client Secret Migration",permalink:"/docs/guides/client-secret-migration"},next:{title:"Integration Testing ASP.NET Core Apps",permalink:"/docs/guides/integration-testing"}},s={},l=[],p={toc:l},u="wrapper";function d(e){let{components:t,...n}=e;return(0,i.yg)(u,(0,r.A)({},p,n,{components:t,mdxType:"MDXLayout"}),(0,i.yg)("h1",{id:"encrypting-client-secrets"},"Encrypting Client Secrets"),(0,i.yg)("p",null,"When installed on Windows, Fig requires client secrets to be encrypted in DPAPI. "),(0,i.yg)("p",null,"This API can be called via Powershell but Fig includes an encryption / decryption utility which is able to perform this operation."),(0,i.yg)("p",null,"Simpliy start the Fig.Dpapi.Client application, generate or enter your client secret and copy the output and add it into an environment variable named ",(0,i.yg)("inlineCode",{parentName:"p"},"Fig_<ClientName>_Secret"),"."),(0,i.yg)("p",null,"Note:"),(0,i.yg)("ul",null,(0,i.yg)("li",{parentName:"ul"},"This can only be run on windows"),(0,i.yg)("li",{parentName:"ul"},"The tool must be running on the same machine as where the encryption key will be used"),(0,i.yg)("li",{parentName:"ul"},"The tool must be running as the same user as the application will run as")))}d.isMDXComponent=!0}}]);