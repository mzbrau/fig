"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[905],{3905:(e,t,i)=>{i.d(t,{Zo:()=>d,kt:()=>g});var n=i(7294);function r(e,t,i){return t in e?Object.defineProperty(e,t,{value:i,enumerable:!0,configurable:!0,writable:!0}):e[t]=i,e}function a(e,t){var i=Object.keys(e);if(Object.getOwnPropertySymbols){var n=Object.getOwnPropertySymbols(e);t&&(n=n.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),i.push.apply(i,n)}return i}function l(e){for(var t=1;t<arguments.length;t++){var i=null!=arguments[t]?arguments[t]:{};t%2?a(Object(i),!0).forEach((function(t){r(e,t,i[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(i)):a(Object(i)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(i,t))}))}return e}function o(e,t){if(null==e)return{};var i,n,r=function(e,t){if(null==e)return{};var i,n,r={},a=Object.keys(e);for(n=0;n<a.length;n++)i=a[n],t.indexOf(i)>=0||(r[i]=e[i]);return r}(e,t);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);for(n=0;n<a.length;n++)i=a[n],t.indexOf(i)>=0||Object.prototype.propertyIsEnumerable.call(e,i)&&(r[i]=e[i])}return r}var s=n.createContext({}),c=function(e){var t=n.useContext(s),i=t;return e&&(i="function"==typeof e?e(t):l(l({},t),e)),i},d=function(e){var t=c(e.components);return n.createElement(s.Provider,{value:t},e.children)},u="mdxType",p={inlineCode:"code",wrapper:function(e){var t=e.children;return n.createElement(n.Fragment,{},t)}},f=n.forwardRef((function(e,t){var i=e.components,r=e.mdxType,a=e.originalType,s=e.parentName,d=o(e,["components","mdxType","originalType","parentName"]),u=c(i),f=r,g=u["".concat(s,".").concat(f)]||u[f]||p[f]||a;return i?n.createElement(g,l(l({ref:t},d),{},{components:i})):n.createElement(g,l({ref:t},d))}));function g(e,t){var i=arguments,r=t&&t.mdxType;if("string"==typeof e||r){var a=i.length,l=new Array(a);l[0]=f;var o={};for(var s in t)hasOwnProperty.call(t,s)&&(o[s]=t[s]);o.originalType=e,o[u]="string"==typeof e?e:r,l[1]=o;for(var c=2;c<a;c++)l[c]=i[c];return n.createElement.apply(null,l)}return n.createElement.apply(null,i)}f.displayName="MDXCreateElement"},6191:(e,t,i)=>{i.r(t),i.d(t,{assets:()=>s,contentTitle:()=>l,default:()=>p,frontMatter:()=>a,metadata:()=>o,toc:()=>c});var n=i(7462),r=(i(7294),i(3905));const a={sidebar_position:9},l="Configuration",o={unversionedId:"features/configuration",id:"features/configuration",title:"Configuration",description:"There are a few parameters which can be configured for Fig which enable or disable certain features in the application.",source:"@site/docs/features/configuration.md",sourceDirName:"features",slug:"/features/configuration",permalink:"/docs/features/configuration",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/features/configuration.md",tags:[],version:"current",sidebarPosition:9,frontMatter:{sidebar_position:9},sidebar:"tutorialSidebar",previous:{title:"Lookup Tables",permalink:"/docs/features/lookup-tables"},next:{title:"Client Management",permalink:"/docs/features/client-management"}},s={},c=[{value:"Allow New Client Registrations",id:"allow-new-client-registrations",level:3},{value:"Allow Updated Client Registrations",id:"allow-updated-client-registrations",level:3},{value:"Allow Offline Settings",id:"allow-offline-settings",level:3},{value:"Allow Dynamic Verifications",id:"allow-dynamic-verifications",level:3},{value:"Allow File Imports",id:"allow-file-imports",level:3},{value:"Allow Client Overrides",id:"allow-client-overrides",level:3},{value:"Client Override Regex",id:"client-override-regex",level:3},{value:"Memory Leak Analysis",id:"memory-leak-analysis",level:3},{value:"Web Application Base Address",id:"web-application-base-address",level:3},{value:"Appearance",id:"appearance",level:2}],d={toc:c},u="wrapper";function p(e){let{components:t,...a}=e;return(0,r.kt)(u,(0,n.Z)({},d,a,{components:t,mdxType:"MDXLayout"}),(0,r.kt)("h1",{id:"configuration"},"Configuration"),(0,r.kt)("p",null,"There are a few parameters which can be configured for Fig which enable or disable certain features in the application."),(0,r.kt)("h3",{id:"allow-new-client-registrations"},"Allow New Client Registrations"),(0,r.kt)("p",null,"When disabled, new client registrations (those who have not previously registered with Fig) will not be allowed to register."),(0,r.kt)("p",null,"It is recommended that new registrations be disabled in a production system once all clients are registered for security reasons."),(0,r.kt)("h3",{id:"allow-updated-client-registrations"},"Allow Updated Client Registrations"),(0,r.kt)("p",null,"When disabled, clients will not be allowed to change the setting definitions when they register."),(0,r.kt)("p",null,"This could be useful in a live upgrade situation where a new version of a client adds settings. Once the new settings have been added, disable updated registrations to avoid any instances of the older clients reverting the registration and removing the new settings."),(0,r.kt)("h3",{id:"allow-offline-settings"},"Allow Offline Settings"),(0,r.kt)("p",null,"Fig clients can save the settings values locally in a file so they client can still start even if the Fig API is down."),(0,r.kt)("p",null,"Settings are encrypted using the client secret and stored in a binary file. However, it may still be desirable to disable this feature if additional security is more important than up time."),(0,r.kt)("h3",{id:"allow-dynamic-verifications"},"Allow Dynamic Verifications"),(0,r.kt)("p",null,"Fig supports plugin and dynamic verifications. Dynamic verifications are defined client side and compiled and run on the server."),(0,r.kt)("p",null,"While a useful feature in some situations, this does allow for remote code execution on the server provided the user is able to register a client and has Fig credentials. It can be disabled for security reasons if it is not required in production."),(0,r.kt)("h3",{id:"allow-file-imports"},"Allow File Imports"),(0,r.kt)("p",null,"Fig supports loading export from an import directory. This is a useful feature when Fig is deployed in a container as a Helm chart or similar can be used to set the initial configuration."),(0,r.kt)("p",null,"However, depending on the level of access to the import directory, it may impose a security risk as imports can be configured to remove existing clients and settings."),(0,r.kt)("h3",{id:"allow-client-overrides"},"Allow Client Overrides"),(0,r.kt)("p",null,"Client overrides allow applications to override the setting value for settings based on an environment variable."),(0,r.kt)("p",null,"This can be useful for container deployments where you want to set container deployment specific values but still see an manage the settings in Fig."),(0,r.kt)("p",null,"Note that any settings changes will be reverted on the next client restart back to the environment variable value."),(0,r.kt)("h3",{id:"client-override-regex"},"Client Override Regex"),(0,r.kt)("p",null,"If client overrides are enabled, only clients with names matching this regular expression will be allowed to override settings."),(0,r.kt)("h3",{id:"memory-leak-analysis"},"Memory Leak Analysis"),(0,r.kt)("p",null,"Fig can analyze memory leaks in running clients. A memory leak is suspected if the memory usage reported by the client has a positive trendline and the average of the final values is more than 1 standard deviation higher than the average of the first values."),(0,r.kt)("p",null,"Fig will discard the first few memory values as the application is ramping up. The period between checks can also be adjusted."),(0,r.kt)("h3",{id:"web-application-base-address"},"Web Application Base Address"),(0,r.kt)("p",null,"This is the address that users use to access the web application. It is used to generate links for web hooks."),(0,r.kt)("h2",{id:"appearance"},"Appearance"),(0,r.kt)("p",null,(0,r.kt)("img",{alt:"image-20220802231541473",src:i(6881).Z,width:"2446",height:"1200"})))}p.isMDXComponent=!0},6881:(e,t,i)=>{i.d(t,{Z:()=>n});const n=i.p+"assets/images/fig-configuration-e129d823236802629424a60235018a8a.png"}}]);