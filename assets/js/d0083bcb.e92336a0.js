"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[4575],{5680:(e,t,n)=>{n.d(t,{xA:()=>p,yg:()=>u});var i=n(6540);function a(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function r(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);t&&(i=i.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,i)}return n}function o(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?r(Object(n),!0).forEach((function(t){a(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):r(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function s(e,t){if(null==e)return{};var n,i,a=function(e,t){if(null==e)return{};var n,i,a={},r=Object.keys(e);for(i=0;i<r.length;i++)n=r[i],t.indexOf(n)>=0||(a[n]=e[n]);return a}(e,t);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);for(i=0;i<r.length;i++)n=r[i],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(a[n]=e[n])}return a}var l=i.createContext({}),g=function(e){var t=i.useContext(l),n=t;return e&&(n="function"==typeof e?e(t):o(o({},t),e)),n},p=function(e){var t=g(e.components);return i.createElement(l.Provider,{value:t},e.children)},c="mdxType",d={inlineCode:"code",wrapper:function(e){var t=e.children;return i.createElement(i.Fragment,{},t)}},m=i.forwardRef((function(e,t){var n=e.components,a=e.mdxType,r=e.originalType,l=e.parentName,p=s(e,["components","mdxType","originalType","parentName"]),c=g(n),m=a,u=c["".concat(l,".").concat(m)]||c[m]||d[m]||r;return n?i.createElement(u,o(o({ref:t},p),{},{components:n})):i.createElement(u,o({ref:t},p))}));function u(e,t){var n=arguments,a=t&&t.mdxType;if("string"==typeof e||a){var r=n.length,o=new Array(r);o[0]=m;var s={};for(var l in t)hasOwnProperty.call(t,l)&&(s[l]=t[l]);s.originalType=e,s[c]="string"==typeof e?e:a,o[1]=s;for(var g=2;g<r;g++)o[g]=n[g];return i.createElement.apply(null,o)}return i.createElement.apply(null,n)}m.displayName="MDXCreateElement"},6214:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>l,contentTitle:()=>o,default:()=>d,frontMatter:()=>r,metadata:()=>s,toc:()=>g});var i=n(8168),a=(n(6540),n(5680));const r={sidebar_position:14},o="Setting Descriptions",s={unversionedId:"features/settings-management/setting-descriptions",id:"features/settings-management/setting-descriptions",title:"Setting Descriptions",description:"Descriptions should be supplied with each setting to explain what the setting does and any potential implications of changing it. Descriptions are provided within the [Setting] attribute.",source:"@site/docs/features/settings-management/setting-descriptions.md",sourceDirName:"features/settings-management",slug:"/features/settings-management/setting-descriptions",permalink:"/docs/features/settings-management/setting-descriptions",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/features/settings-management/setting-descriptions.md",tags:[],version:"current",sidebarPosition:14,frontMatter:{sidebar_position:14},sidebar:"tutorialSidebar",previous:{title:"Configuration Section",permalink:"/docs/features/settings-management/configuration-section"},next:{title:"Display Scripts",permalink:"/docs/features/settings-management/display-scripts"}},l={},g=[{value:"Setting Descriptions from Markdown Files",id:"setting-descriptions-from-markdown-files",level:2},{value:"Images",id:"images",level:2}],p={toc:g},c="wrapper";function d(e){let{components:t,...r}=e;return(0,a.yg)(c,(0,i.A)({},p,r,{components:t,mdxType:"MDXLayout"}),(0,a.yg)("h1",{id:"setting-descriptions"},"Setting Descriptions"),(0,a.yg)("p",null,"Descriptions should be supplied with each setting to explain what the setting does and any potential implications of changing it. Descriptions are provided within the ",(0,a.yg)("inlineCode",{parentName:"p"},"[Setting]")," attribute."),(0,a.yg)("p",null,"A basic description might look like this:"),(0,a.yg)("pre",null,(0,a.yg)("code",{parentName:"pre",className:"language-csharp"},'[Setting("Turns on the debug mode", false)]\npublic bool DebugMode { get; set; }\n')),(0,a.yg)("p",null,(0,a.yg)("img",{alt:"image-20230725221606792",src:n(8130).A,width:"704",height:"322"})),(0,a.yg)("p",null,"However, setting descriptions also support ",(0,a.yg)("strong",{parentName:"p"},"basic Markdown syntax")," which allow developers to convey information in a format that is easy to understand and digest for the person performing the configuration. A detailed description is recommended and may look like this:"),(0,a.yg)("pre",null,(0,a.yg)("code",{parentName:"pre",className:"language-csharp"},'[Setting("**Debug Mode** results in the following changes to the application:\\r\\n" +\n             "- Increases *logging* level\\r\\n" +\n             "- Outputs **full stack traces**\\r\\n" +\n             "- Logs *timings* for different operations \\r\\n" +\n             "\\r\\nExample output with *debug mode* on:\\r\\n" +\n             "```\\r\\nMethod: Do Stuff, Execution Time: 45ms\\r\\n```", false)]\npublic bool DebugMode { get; set; }\n')),(0,a.yg)("p",null,"Which results in a more readable text description:"),(0,a.yg)("p",null,(0,a.yg)("img",{alt:"image-20230725222814110",src:n(6976).A,width:"938",height:"584"})),(0,a.yg)("h2",{id:"setting-descriptions-from-markdown-files"},"Setting Descriptions from Markdown Files"),(0,a.yg)("p",null,"While the example above looks pretty good for the person configuring the application. It it is difficult to read for the developer. An easier way to manage the documentation is to store it in a markdown file which is an embedded resource in the application and then reference it in the fig configuration."),(0,a.yg)("p",null,"Steps are as follows:"),(0,a.yg)("ol",null,(0,a.yg)("li",{parentName:"ol"},"Create a markdown file within the project (entry assembly) and give it a name (it doesn't matter what)"),(0,a.yg)("li",{parentName:"ol"},"Make the markdown file an embedded resource in the project"),(0,a.yg)("li",{parentName:"ol"},"Write your documentation within the markdown file"),(0,a.yg)("li",{parentName:"ol"},"Reference the file within fig using the following syntax:")),(0,a.yg)("pre",null,(0,a.yg)("code",{parentName:"pre",className:"language-csharp"},"$FullyQualifiedResourceName\n")),(0,a.yg)("p",null,"For example"),(0,a.yg)("pre",null,(0,a.yg)("code",{parentName:"pre",className:"language-csharp"},"$Fig.Integration.SqlLookupTableService.ServiceDescription.md\n")),(0,a.yg)("p",null,"However, there might be many settings and in this case you don't what to create a markdown file per setting. Fig allows you to specify a section of a markdown file using the following syntax:"),(0,a.yg)("pre",null,(0,a.yg)("code",{parentName:"pre",className:"language-csharp"},"$FullyQualifiedResourceName#HeadingName\n")),(0,a.yg)("p",null,"For example"),(0,a.yg)("pre",null,(0,a.yg)("code",{parentName:"pre",className:"language-csharp"},"$Fig.Integration.SqlLookupTableService.ServiceDescription.md#FigUri\n")),(0,a.yg)("p",null,"This will take all the text and subheadings below that heading block, but not that heading block itself."),(0,a.yg)("p",null,"You can see a full working example of this ",(0,a.yg)("a",{parentName:"p",href:"https://github.com/mzbrau/fig/blob/main/src/integrations/Fig.Integration.SqlLookupTableService/Settings.cs#L11"},"here"),"."),(0,a.yg)("p",null,"Fig even supports multiple files separated by a comma. For example:"),(0,a.yg)("pre",null,(0,a.yg)("code",{parentName:"pre",className:"language-csharp"},"$Service.ServiceDescription.md#FigUri,$Service.OtherDoc.md\n")),(0,a.yg)("p",null,"Each section can be a full document or part of a document. A line is inserted between documents. Documents are added in the order they are provided."),(0,a.yg)("h2",{id:"images"},"Images"),(0,a.yg)("p",null,"Fig supports displaying images in both setting descriptions and client descriptions."),(0,a.yg)("p",null,"To add images, take the following steps:"),(0,a.yg)("ol",null,(0,a.yg)("li",{parentName:"ol"},"Reference the image in your markdown file e.g. ",(0,a.yg)("inlineCode",{parentName:"li"},"![MyImage](C:\\Temp\\MyImage.png)")),(0,a.yg)("li",{parentName:"ol"},"Add the image as an ",(0,a.yg)("strong",{parentName:"li"},"embedded resource")," in your application"),(0,a.yg)("li",{parentName:"ol"},"Thats it, Fig will do the rest. What happens behind the scenes is that Fig will replace the image path with a base64 encoded version of the image which means it can be embedded in the document. This is the version that is registered with the API.")),(0,a.yg)("p",null,"In the image below, the Fig logo has been added to the markdown file and appears in the setting description."),(0,a.yg)("p",null,(0,a.yg)("img",{alt:"image-20240418211057459",src:n(6559).A,width:"908",height:"960"})))}d.isMDXComponent=!0},8130:(e,t,n)=>{n.d(t,{A:()=>i});const i=n.p+"assets/images/image-20230725221606792-6696fe4e20ac841e16c37d3f57016ce6.png"},6976:(e,t,n)=>{n.d(t,{A:()=>i});const i=n.p+"assets/images/image-20230725222814110-fcf97135177adb7c91ac917a79766333.png"},6559:(e,t,n)=>{n.d(t,{A:()=>i});const i=n.p+"assets/images/image-20240418211057459-c4ce3d0920ce8daece03752d8252a644.png"}}]);