"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[755],{3905:(e,t,n)=>{n.d(t,{Zo:()=>c,kt:()=>g});var i=n(7294);function a(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function r(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);t&&(i=i.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,i)}return n}function o(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?r(Object(n),!0).forEach((function(t){a(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):r(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function l(e,t){if(null==e)return{};var n,i,a=function(e,t){if(null==e)return{};var n,i,a={},r=Object.keys(e);for(i=0;i<r.length;i++)n=r[i],t.indexOf(n)>=0||(a[n]=e[n]);return a}(e,t);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);for(i=0;i<r.length;i++)n=r[i],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(a[n]=e[n])}return a}var s=i.createContext({}),p=function(e){var t=i.useContext(s),n=t;return e&&(n="function"==typeof e?e(t):o(o({},t),e)),n},c=function(e){var t=p(e.components);return i.createElement(s.Provider,{value:t},e.children)},u="mdxType",d={inlineCode:"code",wrapper:function(e){var t=e.children;return i.createElement(i.Fragment,{},t)}},m=i.forwardRef((function(e,t){var n=e.components,a=e.mdxType,r=e.originalType,s=e.parentName,c=l(e,["components","mdxType","originalType","parentName"]),u=p(n),m=a,g=u["".concat(s,".").concat(m)]||u[m]||d[m]||r;return n?i.createElement(g,o(o({ref:t},c),{},{components:n})):i.createElement(g,o({ref:t},c))}));function g(e,t){var n=arguments,a=t&&t.mdxType;if("string"==typeof e||a){var r=n.length,o=new Array(r);o[0]=m;var l={};for(var s in t)hasOwnProperty.call(t,s)&&(l[s]=t[s]);l.originalType=e,l[u]="string"==typeof e?e:a,o[1]=l;for(var p=2;p<r;p++)o[p]=n[p];return i.createElement.apply(null,o)}return i.createElement.apply(null,n)}m.displayName="MDXCreateElement"},2980:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>s,contentTitle:()=>o,default:()=>d,frontMatter:()=>r,metadata:()=>l,toc:()=>p});var i=n(7462),a=(n(7294),n(3905));const r={sidebar_position:2},o="Client Configuration",l={unversionedId:"client-configuration",id:"client-configuration",title:"Client Configuration",description:"Fig can be installed into any client type however this section will focus on asp.net style projects written in dotnet 7.",source:"@site/docs/client-configuration.md",sourceDirName:".",slug:"/client-configuration",permalink:"/docs/client-configuration",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/client-configuration.md",tags:[],version:"current",sidebarPosition:2,frontMatter:{sidebar_position:2},sidebar:"tutorialSidebar",previous:{title:"Introduction",permalink:"/docs/intro"},next:{title:"Overview",permalink:"/docs/category/overview"}},s={},p=[{value:"Fig Options",id:"fig-options",level:2}],c={toc:p},u="wrapper";function d(e){let{components:t,...n}=e;return(0,a.kt)(u,(0,i.Z)({},c,n,{components:t,mdxType:"MDXLayout"}),(0,a.kt)("h1",{id:"client-configuration"},"Client Configuration"),(0,a.kt)("p",null,"Fig can be installed into any client type however this section will focus on asp.net style projects written in dotnet 7."),(0,a.kt)("ol",null,(0,a.kt)("li",{parentName:"ol"},(0,a.kt)("p",{parentName:"li"},"Add the Fig.Client nuget package")),(0,a.kt)("li",{parentName:"ol"},(0,a.kt)("p",{parentName:"li"},"In your program.cs file, add the following"))),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},'var configuration = new ConfigurationBuilder()\n    .AddFig<Settings>(o =>\n    {\n        o.ClientName = "<YOUR CLIENT NAME>";\n    }).Build();\n')),(0,a.kt)("admonition",{title:"My tip",type:"tip"},(0,a.kt)("p",{parentName:"admonition"},"It is recommended that Fig be the ",(0,a.kt)("strong",{parentName:"p"},"LAST")," configuration provider added. This is because Fig will override any settings that are set in other configuration providers. If you use other configuration providers after Fig, they may overwrite settings in the application but this will not be visible from the Fig web application.\nIf you need to override a value locally, you can use the ",(0,a.kt)("a",{parentName:"p",href:"https://www.figsettings.com/docs/features/client-settings-override/"},"client settings override")," feature.")),(0,a.kt)("ol",{start:3},(0,a.kt)("li",{parentName:"ol"},(0,a.kt)("p",{parentName:"li"},"Add an environment variable called FIG_API_URI with the URI of the Fig API. For example:"),(0,a.kt)("pre",{parentName:"li"},(0,a.kt)("code",{parentName:"pre"},"FIG_API_URI=https://localhost:7281\n")))),(0,a.kt)("admonition",{title:"My tip",type:"tip"},(0,a.kt)("p",{parentName:"admonition"},"You can disable Fig by removing the FIG_API_URI environment variable. This is useful if you want to use other configuration providers instead in some environments.")),(0,a.kt)("ol",{start:4},(0,a.kt)("li",{parentName:"ol"},(0,a.kt)("p",{parentName:"li"},"Create a class to hold your configuration items. e.g. ",(0,a.kt)("inlineCode",{parentName:"p"},"Settings")," (they can be called whatever you want)")),(0,a.kt)("li",{parentName:"ol"},(0,a.kt)("p",{parentName:"li"},"Extend ",(0,a.kt)("inlineCode",{parentName:"p"},"Settings")," from ",(0,a.kt)("inlineCode",{parentName:"p"},"SettingsBase"))),(0,a.kt)("li",{parentName:"ol"},(0,a.kt)("p",{parentName:"li"},"Create a secret for your client. This must be a random string of at least 32 characters. Fig accepts 3 ways to register a secret.\nOn ",(0,a.kt)("strong",{parentName:"p"},"Windows"),", secrets must be stored in DPAPI and the encrypted value set in an environment variable called ",(0,a.kt)("inlineCode",{parentName:"p"},"FIG_<CLIENT NAME>_SECRET"),". There is a DPAPI tool included in the Fig repository which can be used to easily generate an encrypted secret.\nIn a ",(0,a.kt)("strong",{parentName:"p"},"Docker Container"),", the secret must be set as a docker secret called ",(0,a.kt)("inlineCode",{parentName:"p"},"FIG_<client name>_SECRET"),".\nOn any other platform, it is possible to specify the secret in code, but this is not recommended for production use. It can be set in the options when registering Fig. e.g."),(0,a.kt)("pre",{parentName:"li"},(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},'var configuration = new ConfigurationBuilder()\n .AddFig<Settings>(o =>\n {\n     o.ClientName = "AspNetApi";\n     o.ClientSecretOverride = "d4b0b76dfb5943f3b0ab6a7f70b6ffa0";\n }).Build();\n'))),(0,a.kt)("li",{parentName:"ol"},(0,a.kt)("p",{parentName:"li"},"It is recommended that you validate the settings when they are changed. To add this functionality, add the following in ",(0,a.kt)("inlineCode",{parentName:"p"},"program.cs"),":"),(0,a.kt)("pre",{parentName:"li"},(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"builder.Host.UseFigValidation<Settings>();\n"))),(0,a.kt)("li",{parentName:"ol"},(0,a.kt)("p",{parentName:"li"},"If you want to allow the Fig web application to be able to restart Fig, add the following in ",(0,a.kt)("inlineCode",{parentName:"p"},"program.cs"),":"),(0,a.kt)("pre",{parentName:"li"},(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},'var configuration = new ConfigurationBuilder()\n .AddFig<Settings>(o =>\n {\n     o.ClientName = "AspNetApi";\n     o.SupportsRestart = true;\n }).Build();\n\nbuilder.Host.UseFigRestart<Settings>();\n')))),(0,a.kt)("h2",{id:"fig-options"},"Fig Options"),(0,a.kt)("p",null,"There are a number of options that you can configure within Fig."),(0,a.kt)("table",null,(0,a.kt)("thead",{parentName:"table"},(0,a.kt)("tr",{parentName:"thead"},(0,a.kt)("th",{parentName:"tr",align:null},"Option"),(0,a.kt)("th",{parentName:"tr",align:null},"Description"),(0,a.kt)("th",{parentName:"tr",align:null},"Example"))),(0,a.kt)("tbody",{parentName:"table"},(0,a.kt)("tr",{parentName:"tbody"},(0,a.kt)("td",{parentName:"tr",align:null},"LiveReload"),(0,a.kt)("td",{parentName:"tr",align:null},"A boolean indicating if this client should live reload its settings. If set to true the values of the properties in the settings class will be updated as soon as they are updated in the Fig web app application. Default to true."),(0,a.kt)("td",{parentName:"tr",align:null},"True")),(0,a.kt)("tr",{parentName:"tbody"},(0,a.kt)("td",{parentName:"tr",align:null},"ClientSecretOverride"),(0,a.kt)("td",{parentName:"tr",align:null},"A string (at least 32 characters) that is unique to this application which is used to authenticate the client towards the Fig api."),(0,a.kt)("td",{parentName:"tr",align:null},"e682dea03f044e0",(0,a.kt)("br",null),"eb571c441eb095ee9")),(0,a.kt)("tr",{parentName:"tbody"},(0,a.kt)("td",{parentName:"tr",align:null},"VersionOverride"),(0,a.kt)("td",{parentName:"tr",align:null},"By default Fig will attempt to locate the version of your application. This is used to display the version within the Fig Web Application. Fig looks at the ",(0,a.kt)("inlineCode",{parentName:"td"},"AssemblyFileVersionAttribute")," for version information. If your application is not versioned in this way, the version can be overriden here."),(0,a.kt)("td",{parentName:"tr",align:null},"1.2")),(0,a.kt)("tr",{parentName:"tbody"},(0,a.kt)("td",{parentName:"tr",align:null},"AllowOfflineSettings"),(0,a.kt)("td",{parentName:"tr",align:null},"True if offline settings should be supported. Offline settings are useful in the case the Fig API is offline and your application needs to start, it can start with the previously issued settings. Settings are stored as an encrypted blob with your client secret as the encryption key. Defaults to true."),(0,a.kt)("td",{parentName:"tr",align:null},"True")))))}d.isMDXComponent=!0}}]);