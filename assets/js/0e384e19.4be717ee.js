"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[9671],{3905:(e,t,n)=>{n.d(t,{Zo:()=>c,kt:()=>m});var r=n(7294);function i(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function a(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);t&&(r=r.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,r)}return n}function o(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?a(Object(n),!0).forEach((function(t){i(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):a(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function l(e,t){if(null==e)return{};var n,r,i=function(e,t){if(null==e)return{};var n,r,i={},a=Object.keys(e);for(r=0;r<a.length;r++)n=a[r],t.indexOf(n)>=0||(i[n]=e[n]);return i}(e,t);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);for(r=0;r<a.length;r++)n=a[r],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(i[n]=e[n])}return i}var p=r.createContext({}),s=function(e){var t=r.useContext(p),n=t;return e&&(n="function"==typeof e?e(t):o(o({},t),e)),n},c=function(e){var t=s(e.components);return r.createElement(p.Provider,{value:t},e.children)},u="mdxType",d={inlineCode:"code",wrapper:function(e){var t=e.children;return r.createElement(r.Fragment,{},t)}},g=r.forwardRef((function(e,t){var n=e.components,i=e.mdxType,a=e.originalType,p=e.parentName,c=l(e,["components","mdxType","originalType","parentName"]),u=s(n),g=i,m=u["".concat(p,".").concat(g)]||u[g]||d[g]||a;return n?r.createElement(m,o(o({ref:t},c),{},{components:n})):r.createElement(m,o({ref:t},c))}));function m(e,t){var n=arguments,i=t&&t.mdxType;if("string"==typeof e||i){var a=n.length,o=new Array(a);o[0]=g;var l={};for(var p in t)hasOwnProperty.call(t,p)&&(l[p]=t[p]);l.originalType=e,l[u]="string"==typeof e?e:i,o[1]=l;for(var s=2;s<a;s++)o[s]=n[s];return r.createElement.apply(null,o)}return r.createElement.apply(null,n)}g.displayName="MDXCreateElement"},9881:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>p,contentTitle:()=>o,default:()=>d,frontMatter:()=>a,metadata:()=>l,toc:()=>s});var r=n(7462),i=(n(7294),n(3905));const a={sidebar_position:1},o="Introduction",l={unversionedId:"intro",id:"intro",title:"Introduction",description:"To get up and running with Fig, you'll need to set up the API, Web and integrate the client nuget package into your application.",source:"@site/docs/intro.md",sourceDirName:".",slug:"/intro",permalink:"/docs/intro",draft:!1,editUrl:"https://github.com/mzbrau/fig/tree/main/doc/fig-documentation/docs/intro.md",tags:[],version:"current",sidebarPosition:1,frontMatter:{sidebar_position:1},sidebar:"tutorialSidebar",next:{title:"Client Configuration",permalink:"/docs/client-configuration"}},p={},s=[{value:"Install API and Web Client",id:"install-api-and-web-client",level:2},{value:"Log in to Web Client",id:"log-in-to-web-client",level:2},{value:"Integrate Client",id:"integrate-client",level:2}],c={toc:s},u="wrapper";function d(e){let{components:t,...n}=e;return(0,i.kt)(u,(0,r.Z)({},c,n,{components:t,mdxType:"MDXLayout"}),(0,i.kt)("h1",{id:"introduction"},"Introduction"),(0,i.kt)("iframe",{width:"100%",height:"450",src:"https://www.youtube.com/embed/H_gFueEYpYs",title:"Introduction to Fig",frameborder:"0",allow:"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share",allowfullscreen:!0}),(0,i.kt)("h1",{id:"quick-start"},"Quick Start"),(0,i.kt)("p",null,"To get up and running with Fig, you'll need to set up the API, Web and integrate the client nuget package into your application."),(0,i.kt)("h2",{id:"install-api-and-web-client"},"Install API and Web Client"),(0,i.kt)("p",null,"The API and Web Clients can be installed using Docker. This guide assumes docker is installed and running."),(0,i.kt)("ol",null,(0,i.kt)("li",{parentName:"ol"},"Clone the ",(0,i.kt)("a",{parentName:"li",href:"https://github.com/mzbrau/fig"},"fig repository")," and use the ",(0,i.kt)("inlineCode",{parentName:"li"},"docker-compose.yml")," file included or copy the code below into a ",(0,i.kt)("inlineCode",{parentName:"li"},"docker-compose.yml")," file.")),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-yaml"},'version: \'3.8\'\n\nservices:\n  fig-api:\n    image: mzbrau/fig-api:latest\n    ports:\n      - "5000:80"\n\n  fig-web:\n    image: mzbrau/fig-web:latest\n    ports:\n      - "8080:80"\n    depends_on:\n      - fig-api\n')),(0,i.kt)("ol",{start:2},(0,i.kt)("li",{parentName:"ol"},"Open a terminal / command prompt, navigate to the directory containing the docker-compose file and type ",(0,i.kt)("inlineCode",{parentName:"li"},"docker-compose up")," to download the containers and run them.")),(0,i.kt)("h2",{id:"log-in-to-web-client"},"Log in to Web Client"),(0,i.kt)("p",null,"Navigate to http://localhost:8080 and at the login prompt enter user: ",(0,i.kt)("inlineCode",{parentName:"p"},"admin")," password: ",(0,i.kt)("inlineCode",{parentName:"p"},"admin"),". You should see the administration view of fig with all options available."),(0,i.kt)("h2",{id:"integrate-client"},"Integrate Client"),(0,i.kt)("admonition",{title:"My tip",type:"tip"},(0,i.kt)("p",{parentName:"admonition"},"In this guide, we'll create an ASP.NET project from scratch and integrate the Fig.Client to use fig for configuration. However the same instructions apply if you have an existing project. Just skip the project creation.")),(0,i.kt)("ol",null,(0,i.kt)("li",{parentName:"ol"},(0,i.kt)("p",{parentName:"li"},"Create new ASP.NET project"),(0,i.kt)("pre",{parentName:"li"},(0,i.kt)("code",{parentName:"pre"},"dotnet new \n"))),(0,i.kt)("li",{parentName:"ol"},(0,i.kt)("p",{parentName:"li"},"Open the project in your favourite")),(0,i.kt)("li",{parentName:"ol"},(0,i.kt)("p",{parentName:"li"},"Add ",(0,i.kt)("strong",{parentName:"p"},(0,i.kt)("a",{parentName:"strong",href:"https://www.nuget.org/packages/Fig.Client"},"Fig.Client"))," nuget package")),(0,i.kt)("li",{parentName:"ol"},(0,i.kt)("p",{parentName:"li"},"Create a new class to hold your application settings, extending the SettingsBase class. For example:"),(0,i.kt)("pre",{parentName:"li"},(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'public interface IExampleSettings\n{\n    string FavouriteAnimal { get; }\n    int FavouriteNumber { get; }\n    bool TrueOrFalse { get; }\n}\n\npublic class ExampleSettings : SettingsBase, IExampleSettings\n{\n    public override string ClientName => "ExampleService";\n\n    [Setting("My favourite animal", "Cow")]\n    public string FavouriteAnimal { get; set; }\n\n    [Setting("My favourite number", 66)]\n    public int FavouriteNumber { get; set; }\n    \n    [Setting("True or false, your choice...", false)]\n    public bool TrueOrFalse { get; set; }\n}\n'))),(0,i.kt)("li",{parentName:"ol"},(0,i.kt)("p",{parentName:"li"},"Register your settings class in the ",(0,i.kt)("inlineCode",{parentName:"p"},"program.cs")," file."),(0,i.kt)("pre",{parentName:"li"},(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'builder.Services.AddFig<ISettings, Settings>(new ConsoleLogger(), options =>\n{\n    options.ApiUri = new Uri("https://localhost:5000"); // Note: This should match the api address and is better stored in the appSettings or as an environment variable.\n    options.ClientSecret = "757bedb7608244c48697710da05db3ca"; // Note: This should be a unique guid and defined elsewhere\n});\n'))),(0,i.kt)("li",{parentName:"ol"},(0,i.kt)("p",{parentName:"li"},"Access the settings class via depedency injection. For example"),(0,i.kt)("pre",{parentName:"li"},(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},"public WeatherForecastController(ILogger<WeatherForecastController> logger, IExampleSettings settings)\n{\n  _logger = logger;\n  _settings = settings;\n}\n"))),(0,i.kt)("li",{parentName:"ol"},(0,i.kt)("p",{parentName:"li"},"Use the settings as required in your application.")),(0,i.kt)("li",{parentName:"ol"},(0,i.kt)("p",{parentName:"li"},"Run your application, the settings will be registered and default values will be used automatically."))),(0,i.kt)("p",null,"See the ",(0,i.kt)("strong",{parentName:"p"},"examples folder")," in the source repository for more examples."))}d.isMDXComponent=!0}}]);