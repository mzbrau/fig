"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[7842],{3905:(e,t,n)=>{n.d(t,{Zo:()=>u,kt:()=>m});var r=n(7294);function i(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function s(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);t&&(r=r.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,r)}return n}function a(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?s(Object(n),!0).forEach((function(t){i(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):s(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function o(e,t){if(null==e)return{};var n,r,i=function(e,t){if(null==e)return{};var n,r,i={},s=Object.keys(e);for(r=0;r<s.length;r++)n=s[r],t.indexOf(n)>=0||(i[n]=e[n]);return i}(e,t);if(Object.getOwnPropertySymbols){var s=Object.getOwnPropertySymbols(e);for(r=0;r<s.length;r++)n=s[r],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(i[n]=e[n])}return i}var c=r.createContext({}),l=function(e){var t=r.useContext(c),n=t;return e&&(n="function"==typeof e?e(t):a(a({},t),e)),n},u=function(e){var t=l(e.components);return r.createElement(c.Provider,{value:t},e.children)},p="mdxType",f={inlineCode:"code",wrapper:function(e){var t=e.children;return r.createElement(r.Fragment,{},t)}},d=r.forwardRef((function(e,t){var n=e.components,i=e.mdxType,s=e.originalType,c=e.parentName,u=o(e,["components","mdxType","originalType","parentName"]),p=l(n),d=i,m=p["".concat(c,".").concat(d)]||p[d]||f[d]||s;return n?r.createElement(m,a(a({ref:t},u),{},{components:n})):r.createElement(m,a({ref:t},u))}));function m(e,t){var n=arguments,i=t&&t.mdxType;if("string"==typeof e||i){var s=n.length,a=new Array(s);a[0]=d;var o={};for(var c in t)hasOwnProperty.call(t,c)&&(o[c]=t[c]);o.originalType=e,o[p]="string"==typeof e?e:i,a[1]=o;for(var l=2;l<s;l++)a[l]=n[l];return r.createElement.apply(null,a)}return r.createElement.apply(null,n)}d.displayName="MDXCreateElement"},9979:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>c,contentTitle:()=>a,default:()=>f,frontMatter:()=>s,metadata:()=>o,toc:()=>l});var r=n(7462),i=(n(7294),n(3905));const s={sidebar_position:4},a="Verifications",o={unversionedId:"features/verifications",id:"features/verifications",title:"Verifications",description:"Fig includes a framework for verifying setting values from within the UI. The setting verifications occur within the Fig API.",source:"@site/docs/features/verifications.md",sourceDirName:"features",slug:"/features/verifications",permalink:"/docs/features/verifications",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/features/verifications.md",tags:[],version:"current",sidebarPosition:4,frontMatter:{sidebar_position:4},sidebar:"tutorialSidebar",previous:{title:"Setting Descriptions",permalink:"/docs/features/settings-management/setting-descriptions"},next:{title:"Event History",permalink:"/docs/features/event-history"}},c={},l=[{value:"Usage",id:"usage",level:3},{value:"Example",id:"example",level:3}],u={toc:l},p="wrapper";function f(e){let{components:t,...n}=e;return(0,i.kt)(p,(0,r.Z)({},u,n,{components:t,mdxType:"MDXLayout"}),(0,i.kt)("h1",{id:"verifications"},"Verifications"),(0,i.kt)("p",null,"Fig includes a framework for verifying setting values from within the UI. The setting verifications occur within the Fig API."),(0,i.kt)("p",null,"Verifications are defined in a dll file placed in a folder called 'plugins' within the base directory of the API. "),(0,i.kt)("p",null,"The plugin definition must implement in the ",(0,i.kt)("inlineCode",{parentName:"p"},"ISettingVerifier")," interface which is defined in the ",(0,i.kt)("inlineCode",{parentName:"p"},"Fig.Api.SettingVerification.Sdk")," project. Verifiers can recieve the values of more than one setting which are passed in as a list of object params into the verifier."),(0,i.kt)("p",null,"Many verifications can be defined within the same assembly."),(0,i.kt)("p",null,"Fig comes with some built in plug in verifications including ",(0,i.kt)("inlineCode",{parentName:"p"},"PingVerifier")," and ",(0,i.kt)("inlineCode",{parentName:"p"},"Rest200OkVerifier"),"."),(0,i.kt)("h3",{id:"usage"},"Usage"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]\npublic class ProductService : SettingsBase\n{\n    public override string ClientName => "ProductService";\n\n    [Setting("This is the address of a website", "http://www.google.com")]\n    public string WebsiteAddress { get; set; }\n}\n')),(0,i.kt)("h3",{id:"example"},"Example"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'public class Rest200OkVerifier : ISettingVerifier\n{\n    public string Name => "Rest200OkVerifier";\n\n    public string Description =>\n        "Makes a GET request to the provided endpoint. " +\n        "Result is considered success if a status code 200 Ok response is received";\n    \n    public VerificationResult RunVerification(params object[] parameters)\n    {\n        if (parameters.Length != 1 || string.IsNullOrEmpty(parameters[0] as string))\n        {\n            return VerificationResult.IncorrectParameters();\n        }\n\n        var result = new VerificationResult();\n        var uri = parameters[0] as string;\n\n        using HttpClient client = new HttpClient();\n        \n        result.AddLog($"Performing get request to address: {uri}");\n        var requestResult = client.GetAsync(uri).Result;\n\n        if (requestResult.StatusCode == HttpStatusCode.OK)\n        {\n            result.Message = "Succeeded";\n            result.Success = true;\n            return result;\n        }\n    \n        result.AddLog($"Request failed. {requestResult.StatusCode}. {requestResult.ReasonPhrase}");\n        result.Message = $"Failed with response: {requestResult.StatusCode}";\n        return result;\n    }\n}\n')))}f.isMDXComponent=!0}}]);