"use strict";(self.webpackChunkfig_documentation=self.webpackChunkfig_documentation||[]).push([[7918],{7684:(e,t,a)=>{a.d(t,{Z:()=>h});var n=a(7462),l=a(7294),s=a(6010),r=a(2802),o=a(8596),c=a(5281),i=a(9960),d=a(4996),m=a(5999);function u(e){return l.createElement("svg",(0,n.Z)({viewBox:"0 0 24 24"},e),l.createElement("path",{d:"M10 19v-5h4v5c0 .55.45 1 1 1h3c.55 0 1-.45 1-1v-7h1.7c.46 0 .68-.57.33-.87L12.67 3.6c-.38-.34-.96-.34-1.34 0l-8.36 7.53c-.34.3-.13.87.33.87H5v7c0 .55.45 1 1 1h3c.55 0 1-.45 1-1z",fill:"currentColor"}))}const b={breadcrumbsContainer:"breadcrumbsContainer_Z_bl",breadcrumbHomeIcon:"breadcrumbHomeIcon_OVgt"};function p(e){let{children:t,href:a,isLast:n}=e;const s="breadcrumbs__link";return n?l.createElement("span",{className:s,itemProp:"name"},t):a?l.createElement(i.Z,{className:s,href:a,itemProp:"item"},l.createElement("span",{itemProp:"name"},t)):l.createElement("span",{className:s},t)}function E(e){let{children:t,active:a,index:r,addMicrodata:o}=e;return l.createElement("li",(0,n.Z)({},o&&{itemScope:!0,itemProp:"itemListElement",itemType:"https://schema.org/ListItem"},{className:(0,s.Z)("breadcrumbs__item",{"breadcrumbs__item--active":a})}),t,l.createElement("meta",{itemProp:"position",content:String(r+1)}))}function g(){const e=(0,d.Z)("/");return l.createElement("li",{className:"breadcrumbs__item"},l.createElement(i.Z,{"aria-label":(0,m.I)({id:"theme.docs.breadcrumbs.home",message:"Home page",description:"The ARIA label for the home page in the breadcrumbs"}),className:(0,s.Z)("breadcrumbs__link",b.breadcrumbsItemLink),href:e},l.createElement(u,{className:b.breadcrumbHomeIcon})))}function h(){const e=(0,r.s1)(),t=(0,o.Ns)();return e?l.createElement("nav",{className:(0,s.Z)(c.k.docs.docBreadcrumbs,b.breadcrumbsContainer),"aria-label":(0,m.I)({id:"theme.docs.breadcrumbs.navAriaLabel",message:"Breadcrumbs",description:"The ARIA label for the breadcrumbs"})},l.createElement("ul",{className:"breadcrumbs",itemScope:!0,itemType:"https://schema.org/BreadcrumbList"},t&&l.createElement(g,null),e.map(((t,a)=>{const n=a===e.length-1;return l.createElement(E,{key:a,active:n,index:a,addMicrodata:!!t.href},l.createElement(p,{href:t.href,isLast:n},t.label))})))):null}},4533:(e,t,a)=>{a.r(t),a.d(t,{default:()=>F});var n=a(7294),l=a(6010),s=a(833),r=a(7524),o=a(5281),c=a(4966),i=a(3120),d=a(4364),m=a(5999);function u(e){let{lastUpdatedAt:t,formattedLastUpdatedAt:a}=e;return n.createElement(m.Z,{id:"theme.lastUpdated.atDate",description:"The words used to describe on which date a page has been last updated",values:{date:n.createElement("b",null,n.createElement("time",{dateTime:new Date(1e3*t).toISOString()},a))}}," on {date}")}function b(e){let{lastUpdatedBy:t}=e;return n.createElement(m.Z,{id:"theme.lastUpdated.byUser",description:"The words used to describe by who the page has been last updated",values:{user:n.createElement("b",null,t)}}," by {user}")}function p(e){let{lastUpdatedAt:t,formattedLastUpdatedAt:a,lastUpdatedBy:l}=e;return n.createElement("span",{className:o.k.common.lastUpdated},n.createElement(m.Z,{id:"theme.lastUpdated.lastUpdatedAtBy",description:"The sentence used to display when a page has been last updated, and by who",values:{atDate:t&&a?n.createElement(u,{lastUpdatedAt:t,formattedLastUpdatedAt:a}):"",byUser:l?n.createElement(b,{lastUpdatedBy:l}):""}},"Last updated{atDate}{byUser}"),!1)}var E=a(7462);const g={iconEdit:"iconEdit_eYIM"};function h(e){let{className:t,...a}=e;return n.createElement("svg",(0,E.Z)({fill:"currentColor",height:"20",width:"20",viewBox:"0 0 40 40",className:(0,l.Z)(g.iconEdit,t),"aria-hidden":"true"},a),n.createElement("g",null,n.createElement("path",{d:"m34.5 11.7l-3 3.1-6.3-6.3 3.1-3q0.5-0.5 1.2-0.5t1.1 0.5l3.9 3.9q0.5 0.4 0.5 1.1t-0.5 1.2z m-29.5 17.1l18.4-18.5 6.3 6.3-18.4 18.4h-6.3v-6.2z"})))}function v(e){let{editUrl:t}=e;return n.createElement("a",{href:t,target:"_blank",rel:"noreferrer noopener",className:o.k.common.editThisPage},n.createElement(h,null),n.createElement(m.Z,{id:"theme.common.editThisPage",description:"The link label to edit the current page"},"Edit this page"))}var f=a(9960);const Z={tag:"tag_zVej",tagRegular:"tagRegular_sFm0",tagWithCount:"tagWithCount_h2kH"};function N(e){let{permalink:t,label:a,count:s}=e;return n.createElement(f.Z,{href:t,className:(0,l.Z)(Z.tag,s?Z.tagWithCount:Z.tagRegular)},a,s&&n.createElement("span",null,s))}const _={tags:"tags_jXut",tag:"tag_QGVx"};function L(e){let{tags:t}=e;return n.createElement(n.Fragment,null,n.createElement("b",null,n.createElement(m.Z,{id:"theme.tags.tagsListLabel",description:"The label alongside a tag list"},"Tags:")),n.createElement("ul",{className:(0,l.Z)(_.tags,"padding--none","margin-left--sm")},t.map((e=>{let{label:t,permalink:a}=e;return n.createElement("li",{key:a,className:_.tag},n.createElement(N,{label:t,permalink:a}))}))))}const k={lastUpdated:"lastUpdated_vbeJ"};function C(e){return n.createElement("div",{className:(0,l.Z)(o.k.docs.docFooterTagsRow,"row margin-bottom--sm")},n.createElement("div",{className:"col"},n.createElement(L,e)))}function T(e){let{editUrl:t,lastUpdatedAt:a,lastUpdatedBy:s,formattedLastUpdatedAt:r}=e;return n.createElement("div",{className:(0,l.Z)(o.k.docs.docFooterEditMetaRow,"row")},n.createElement("div",{className:"col"},t&&n.createElement(v,{editUrl:t})),n.createElement("div",{className:(0,l.Z)("col",k.lastUpdated)},(a||s)&&n.createElement(p,{lastUpdatedAt:a,formattedLastUpdatedAt:r,lastUpdatedBy:s})))}function U(e){const{content:t}=e,{metadata:a}=t,{editUrl:s,lastUpdatedAt:r,formattedLastUpdatedAt:c,lastUpdatedBy:i,tags:d}=a,m=d.length>0,u=!!(s||r||i);return m||u?n.createElement("footer",{className:(0,l.Z)(o.k.docs.docFooter,"docusaurus-mt-lg")},m&&n.createElement(C,{tags:d}),u&&n.createElement(T,{editUrl:s,lastUpdatedAt:r,lastUpdatedBy:i,formattedLastUpdatedAt:c})):null}var w=a(9407),y=a(6043),A=a(3743);const x={tocCollapsibleButton:"tocCollapsibleButton_TO0P",tocCollapsibleButtonExpanded:"tocCollapsibleButtonExpanded_MG3E"};function B(e){let{collapsed:t,...a}=e;return n.createElement("button",(0,E.Z)({type:"button"},a,{className:(0,l.Z)("clean-btn",x.tocCollapsibleButton,!t&&x.tocCollapsibleButtonExpanded,a.className)}),n.createElement(m.Z,{id:"theme.TOCCollapsible.toggleButtonLabel",description:"The label used by the button on the collapsible TOC component"},"On this page"))}const I={tocCollapsible:"tocCollapsible_ETCw",tocCollapsibleContent:"tocCollapsibleContent_vkbj",tocCollapsibleExpanded:"tocCollapsibleExpanded_sAul"};function M(e){let{toc:t,className:a,minHeadingLevel:s,maxHeadingLevel:r}=e;const{collapsed:o,toggleCollapsed:c}=(0,y.u)({initialState:!0});return n.createElement("div",{className:(0,l.Z)(I.tocCollapsible,!o&&I.tocCollapsibleExpanded,a)},n.createElement(B,{collapsed:o,onClick:c}),n.createElement(y.z,{lazy:!0,className:I.tocCollapsibleContent,collapsed:o},n.createElement(A.Z,{toc:t,minHeadingLevel:s,maxHeadingLevel:r})))}var H=a(2503),V=a(7684),P=a(3548);const S={docItemContainer:"docItemContainer_Adtb",docItemCol:"docItemCol_GujU",tocMobile:"tocMobile_aoJ5"};function D(e){const{content:t}=e,{metadata:a,frontMatter:l,assets:r}=t,{keywords:o}=l,{description:c,title:i}=a,d=r.image??l.image;return n.createElement(s.d,{title:i,description:c,keywords:o,image:d})}function R(e){const{content:t}=e,{metadata:a,frontMatter:s}=t,{hide_title:m,hide_table_of_contents:u,toc_min_heading_level:b,toc_max_heading_level:p}=s,{title:E}=a,g=!m&&void 0===t.contentTitle,h=(0,r.i)(),v=!u&&t.toc&&t.toc.length>0,f=v&&("desktop"===h||"ssr"===h);return n.createElement("div",{className:"row"},n.createElement("div",{className:(0,l.Z)("col",!u&&S.docItemCol)},n.createElement(i.Z,null),n.createElement("div",{className:S.docItemContainer},n.createElement("article",null,n.createElement(V.Z,null),n.createElement(d.Z,null),v&&n.createElement(M,{toc:t.toc,minHeadingLevel:b,maxHeadingLevel:p,className:(0,l.Z)(o.k.docs.docTocMobile,S.tocMobile)}),n.createElement("div",{className:(0,l.Z)(o.k.docs.docMarkdown,"markdown")},g&&n.createElement("header",null,n.createElement(H.Z,{as:"h1"},E)),n.createElement(P.Z,null,n.createElement(t,null))),n.createElement(U,e)),n.createElement(c.Z,{previous:a.previous,next:a.next}))),f&&n.createElement("div",{className:"col col--3"},n.createElement(w.Z,{toc:t.toc,minHeadingLevel:b,maxHeadingLevel:p,className:o.k.docs.docTocDesktop})))}function F(e){const t=`docs-doc-id-${e.content.metadata.unversionedId}`;return n.createElement(s.FG,{className:t},n.createElement(D,e),n.createElement(R,e))}},4966:(e,t,a)=>{a.d(t,{Z:()=>i});var n=a(7462),l=a(7294),s=a(5999),r=a(6010),o=a(9960);function c(e){const{permalink:t,title:a,subLabel:n,isNext:s}=e;return l.createElement(o.Z,{className:(0,r.Z)("pagination-nav__link",s?"pagination-nav__link--next":"pagination-nav__link--prev"),to:t},n&&l.createElement("div",{className:"pagination-nav__sublabel"},n),l.createElement("div",{className:"pagination-nav__label"},a))}function i(e){const{previous:t,next:a}=e;return l.createElement("nav",{className:"pagination-nav docusaurus-mt-lg","aria-label":(0,s.I)({id:"theme.docs.paginator.navAriaLabel",message:"Docs pages navigation",description:"The ARIA label for the docs pagination"})},t&&l.createElement(c,(0,n.Z)({},t,{subLabel:l.createElement(s.Z,{id:"theme.docs.paginator.previous",description:"The label used to navigate to the previous doc"},"Previous")})),a&&l.createElement(c,(0,n.Z)({},a,{subLabel:l.createElement(s.Z,{id:"theme.docs.paginator.next",description:"The label used to navigate to the next doc"},"Next"),isNext:!0})))}},4364:(e,t,a)=>{a.d(t,{Z:()=>c});var n=a(7294),l=a(6010),s=a(5999),r=a(4477),o=a(5281);function c(e){let{className:t}=e;const a=(0,r.E)();return a.badge?n.createElement("span",{className:(0,l.Z)(t,o.k.docs.docVersionBadge,"badge badge--secondary")},n.createElement(s.Z,{id:"theme.docs.versionBadge.label",values:{versionLabel:a.label}},"Version: {versionLabel}")):null}},3120:(e,t,a)=>{a.d(t,{Z:()=>g});var n=a(7294),l=a(6010),s=a(2263),r=a(9960),o=a(5999),c=a(143),i=a(373),d=a(5281),m=a(4477);const u={unreleased:function(e){let{siteTitle:t,versionMetadata:a}=e;return n.createElement(o.Z,{id:"theme.docs.versions.unreleasedVersionLabel",description:"The label used to tell the user that he's browsing an unreleased doc version",values:{siteTitle:t,versionLabel:n.createElement("b",null,a.label)}},"This is unreleased documentation for {siteTitle} {versionLabel} version.")},unmaintained:function(e){let{siteTitle:t,versionMetadata:a}=e;return n.createElement(o.Z,{id:"theme.docs.versions.unmaintainedVersionLabel",description:"The label used to tell the user that he's browsing an unmaintained doc version",values:{siteTitle:t,versionLabel:n.createElement("b",null,a.label)}},"This is documentation for {siteTitle} {versionLabel}, which is no longer actively maintained.")}};function b(e){const t=u[e.versionMetadata.banner];return n.createElement(t,e)}function p(e){let{versionLabel:t,to:a,onClick:l}=e;return n.createElement(o.Z,{id:"theme.docs.versions.latestVersionSuggestionLabel",description:"The label used to tell the user to check the latest version",values:{versionLabel:t,latestVersionLink:n.createElement("b",null,n.createElement(r.Z,{to:a,onClick:l},n.createElement(o.Z,{id:"theme.docs.versions.latestVersionLinkLabel",description:"The label used for the latest version suggestion link label"},"latest version")))}},"For up-to-date documentation, see the {latestVersionLink} ({versionLabel}).")}function E(e){let{className:t,versionMetadata:a}=e;const{siteConfig:{title:r}}=(0,s.Z)(),{pluginId:o}=(0,c.gA)({failfast:!0}),{savePreferredVersionName:m}=(0,i.J)(o),{latestDocSuggestion:u,latestVersionSuggestion:E}=(0,c.Jo)(o),g=u??(h=E).docs.find((e=>e.id===h.mainDocId));var h;return n.createElement("div",{className:(0,l.Z)(t,d.k.docs.docVersionBanner,"alert alert--warning margin-bottom--md"),role:"alert"},n.createElement("div",null,n.createElement(b,{siteTitle:r,versionMetadata:a})),n.createElement("div",{className:"margin-top--md"},n.createElement(p,{versionLabel:E.label,to:g.path,onClick:()=>m(E.name)})))}function g(e){let{className:t}=e;const a=(0,m.E)();return a.banner?n.createElement(E,{className:t,versionMetadata:a}):null}}}]);