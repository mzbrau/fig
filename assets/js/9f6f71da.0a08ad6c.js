"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[9317],{3905:(e,n,t)=>{t.d(n,{Zo:()=>u,kt:()=>g});var r=t(7294);function o(e,n,t){return n in e?Object.defineProperty(e,n,{value:t,enumerable:!0,configurable:!0,writable:!0}):e[n]=t,e}function i(e,n){var t=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);n&&(r=r.filter((function(n){return Object.getOwnPropertyDescriptor(e,n).enumerable}))),t.push.apply(t,r)}return t}function a(e){for(var n=1;n<arguments.length;n++){var t=null!=arguments[n]?arguments[n]:{};n%2?i(Object(t),!0).forEach((function(n){o(e,n,t[n])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(t)):i(Object(t)).forEach((function(n){Object.defineProperty(e,n,Object.getOwnPropertyDescriptor(t,n))}))}return e}function s(e,n){if(null==e)return{};var t,r,o=function(e,n){if(null==e)return{};var t,r,o={},i=Object.keys(e);for(r=0;r<i.length;r++)t=i[r],n.indexOf(t)>=0||(o[t]=e[t]);return o}(e,n);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);for(r=0;r<i.length;r++)t=i[r],n.indexOf(t)>=0||Object.prototype.propertyIsEnumerable.call(e,t)&&(o[t]=e[t])}return o}var c=r.createContext({}),l=function(e){var n=r.useContext(c),t=n;return e&&(t="function"==typeof e?e(n):a(a({},n),e)),t},u=function(e){var n=l(e.components);return r.createElement(c.Provider,{value:n},e.children)},p="mdxType",d={inlineCode:"code",wrapper:function(e){var n=e.children;return r.createElement(r.Fragment,{},n)}},f=r.forwardRef((function(e,n){var t=e.components,o=e.mdxType,i=e.originalType,c=e.parentName,u=s(e,["components","mdxType","originalType","parentName"]),p=l(t),f=o,g=p["".concat(c,".").concat(f)]||p[f]||d[f]||i;return t?r.createElement(g,a(a({ref:n},u),{},{components:t})):r.createElement(g,a({ref:n},u))}));function g(e,n){var t=arguments,o=n&&n.mdxType;if("string"==typeof e||o){var i=t.length,a=new Array(i);a[0]=f;var s={};for(var c in n)hasOwnProperty.call(n,c)&&(s[c]=n[c]);s.originalType=e,s[p]="string"==typeof e?e:o,a[1]=s;for(var l=2;l<i;l++)a[l]=t[l];return r.createElement.apply(null,a)}return r.createElement.apply(null,t)}f.displayName="MDXCreateElement"},6189:(e,n,t)=>{t.r(n),t.d(n,{assets:()=>c,contentTitle:()=>a,default:()=>d,frontMatter:()=>i,metadata:()=>s,toc:()=>l});var r=t(7462),o=(t(7294),t(3905));const i={sidebar_position:10},a="API Configuration",s={unversionedId:"api-configuration",id:"api-configuration",title:"API Configuration",description:"The Fig API can be configured with the following sources:",source:"@site/docs/api-configuration.md",sourceDirName:".",slug:"/api-configuration",permalink:"/docs/api-configuration",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/api-configuration.md",tags:[],version:"current",sidebarPosition:10,frontMatter:{sidebar_position:10},sidebar:"tutorialSidebar",previous:{title:"Acknowledgements",permalink:"/docs/acknowledgements"},next:{title:"Error Monitoring",permalink:"/docs/error-monitoring"}},c={},l=[],u={toc:l},p="wrapper";function d(e){let{components:n,...t}=e;return(0,o.kt)(p,(0,r.Z)({},u,t,{components:n,mdxType:"MDXLayout"}),(0,o.kt)("h1",{id:"api-configuration"},"API Configuration"),(0,o.kt)("p",null,"The Fig API can be configured with the following sources:"),(0,o.kt)("ul",null,(0,o.kt)("li",{parentName:"ul"},"appsettings.json file"),(0,o.kt)("li",{parentName:"ul"},"environment variables"),(0,o.kt)("li",{parentName:"ul"},"docker secrets")),(0,o.kt)("p",null,"There are the following settings:"),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-json"},'"ApiSettings": {\n    // The connection string for the database\n    "DbConnectionString": "Data Source=fig.db;Version=3;New=True",\n  \n    // A secret value used to sign auth tokens and encrypt data in the database. Should be long.\n    "Secret": "76d3bd66ddb74623ad38e39d7eae6ee5da28bbdce9aa40209d0decf630777304",\n  \n    // Lifetime of auth tokens in minutes\n    "TokenLifeMinutes": 10080,\n  \n    // Previous secret value is used when migrating from an old to new secret. See API Secret Migration.\n    "PreviousSecret": "",\n  \n    // True if the secret and previous secret values are encrypted via dpapi. If false, the raw values are used.\n    "SecretsDpapiEncrypted": false,\n    \n    // Addresses of the web client. This is used for CORS validation.\n    "WebClientAddresses": [\n        "https://localhost:7148",\n        "http://localhost:8080",\n        "http://localhost:5050"\n      ],\n    \n    // True the API should enforce a password change on the default admin user on first login.\n    "ForceAdminDefaultPasswordChange": false,\n\n    // the DSN value for sentry which is used for error monitoring\n    "SentryDsn": "",\n\n    // The sample rate for sentry. This can be turned down if too many errors are being sent.\n    "SentrySampleRate": 1.0\n  },\n')))}d.isMDXComponent=!0}}]);