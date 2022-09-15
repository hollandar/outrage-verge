# Outrage.Verge

A static site generator, for those who speak HTML and CSS, for life on the edge.  Deploy the output to GitHub pages, Netlify, AmazonS3 or any other static host.

## Getting Started

1. Make sure you have the dotnet command line installed.
2. Clone the starting repository using `git clone --recursive https://github.com/hollandar/outrage-verge-start mysite`
3. Execute `/serve.ps1` to build the site, service it locally and watch for changes.
4. Start hacking your own site.

If you have reason to change verge, its base library or the theme, consider sending us an early Pull Request.

## Templates

All templates are built in HTML and can be modified using CSS.  And you can inject any javascript or CSS libraries into the build process.

A simple HTML page from the start site:
```html
<Document title="Home" />
<DefineSection name="body">
    <TwoUp>
        <Left>
            <div style="padding: 8rem; text-align: center;">
                <h1>Verge Start</h1>
                <h2>Welcome to Verge.</h2>
                <p>
                    Verge is a static site builder for the edge, for serverless.
                </p>
                <p>
                    You already speak HTML and CSS, so its a small step to build websites quickly using Verge by reusing
                    visual elements.
                    Elements come from the underlying library, the theme, or are in your own site.
                </p>
                <p>
                    Integrating front end libraries, like Patternfly via NPM is very easy.
                    See the Patternfly theme for an example, start with theme.yaml, the theme configuration.
                </p>
                <div class="pf-c-action-list">
                    <div class="pf-c-action-list__item">
                        <a class="pf-c-button pf-m-primary" href="http://github.com/hollandar/outrage-verge-start">Verge
                            Start</a>
                    </div>
                    <div class="pf-c-action-list__item">
                        <a class="pf-c-button pf-m-primary" href="http://github.com/hollandar/outrage-verge">Verge</a>
                    </div>
                    <div class="pf-c-action-list__item">
                        <a class="pf-c-button pf-m-secondary" href="https://www.patternfly.org/v4/">Patternfly V4</a>
                    </div>
                    <div class="pf-c-action-list__item"></div>
                </div>
            </div>
        </Left>
        <Right>
            <Picture src="/static/intro.jpg" />
        </Right>
    </TwoUp>

    <div class="patternfly-content">
        <h2>
            Verge Start Structure
        </h2>
        <p>You will find a couple of Git submodules in this repository:</p>
        <ol>
            <li><strong>lib</strong> - A set of base pages that every Verge site needs.</li>
            <li><strong>themes/patternfly</strong> - A set of specific pages for the Patternfly theme.</li>
            <li><strong>site</strong> - Your content.</li>
        </ol>
        <p>When Verge looks for content, it searches site first, then theme, then lib. Copy anything you want to change
            into your site folder in the same location. You can change everything.</p>
        <p>If you have proposals for changes to the underlying library, or changes to the Patternfly theme, you can
            always propose a pull request.</p>


    </div>
</DefineSection>
```

Its pure HTML with some CSS Styling.

We also support a page processor that handles markdown (.md):
```markdown
title: About Verge
---
# Verge

Verge is a static site generator with simple HTML templates and components.  It has a very open and extensible build process.
```
## Sections

A section is a location into which content is inserted.  If you look at the layout.t.html page, which is the configuration for all generated HTML pages, it includes body, script and head sections:
```html
<!DOCTYPE html>
<html lang="$(language)">
    <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>$(siteName) - $(title)</title>
        <Section name="head" />
    </head>
    <body>
        <Section name="body" required="true"/>
        <Section name="script" />
    </body>
</html>
```

When you define a section, its content is inserted into that location in this layout, but sections can be added to at different levels in the templates.  Here, patternfly adds its CSS links.  The effect of this is that the theme links are rendered into head, and then your links.

This works the same way as in the Razor language.

```
<DefineSection name="head">
    <link rel="stylesheet" href="/theme/patternfly/css/patternfly.min.css" />
    <link rel="stylesheet" href="/theme/patternfly/css/theme.css" />
    <Section name="head"/>
</DefineSection>
```

For the markdown page source, it uses a parent template called `markdown.t.html` and the generated HTML is inserted into the `body` section.

## Interceptors and Components

An interceptor takes the attributes and the inner content from a tag, and allows you to transform it in any way you want.  You could:
 * Have it pull data from a web service during the build process.
 * Load data from the database and provide it via a variable accessible within pages.
 * Load data from the site repository and process it in any way you like, although there is already a Json interceptor that can do that.

The underlying library with initial components can be found at [outrage-verge-lib](https://github.com/hollandar/outrage-verge-lib).
Begin by cloning [outrage-verge-start](https://github.com/hollandar/outrage-verge-start) which contains submodules for verge, the lib and a Patternfly theme [outrage-verge-theme-patternfly](https://github.com/hollandar/outrage-verge-theme-patternfly].

Components are HTML only and are defined in components.yaml in your site, in the theme or in the library, and their content can be overridden at any level.  A fallback search for components happens at build time, so content from your site overrides anything in the theme, which overrides anything in the library.

One of the simplest components is the TwoUp component.  It is HTML only, has no additional processing and in the library also has no styling.  Some styling is added by the Patternfly theme, and more can be added by your site, by targeting the CSS selectors.
```html
<div class="lib-twoup">
    <div class="lib-twoup-left">
        <Slot name="Left"/>
    </div>
    <div class="lib-twoup-right">
        <Slot name="Right"/>
    </div>
</div>
```

Using it on your site is as simple as the following, which is also how you would configure you home page, or in fact any page:
```html
<Document title="Home"/>
<DefineSection name="body">
    <TwoUp>
        <Left>Content on the left</Left>
        <Right>Content on the right</Right>
    </TwoUp>
</DefineSection>
```

An Interceptor can interrupt the processing of a certain tag, and do something about it, it can:
 * Emit a stream of html tokens.
 * Parse tokens from an html file and emit them.
 * Emit raw html.
 * Or emit nothing and push values into variables the page can use.

The `ForEach` interceptor is a great example of this, combined with the `Json` interceptor, it is how the menu rendering in the Patternfly theme works.

In the HTML for the Patternfly theme, you will find the following markup which uses a few interceptors:
```html
<Json name="menu" from="menu.json">
    <ul class="pf-c-nav__list">
        <ForEach name="item" in="menu.menuItems">
            <OnLink uri="$(item.link)" currentClass="pf-m-current">
                <li class="pf-c-nav__item $(_currentClass)">
                    <a href="$(_uri)" class="pf-c-nav__link">$(item.label)</a>
                </li>
            </OnLink>
        </ForEach>
    </ul>
</Json>
```

The `Json` interceptor will deserialize JSON from a content item called `menu.json` which you define.  It stores the content into a variable called `menu`.

The `ForEach` interceptor then repeats its content for every item in the `menu.menuItems` collection, storing each item in the `item` variable.

The `OnLink` interceptor then optionally injects the `currentClass` variable IF we are current on the link specified by the menu item.

Variables in the HTML are defined by interceptors and used as follows: `$(variableName)`.  By convention, if a parameter is passed to an interceptor, anything that is translated to a variable receives the _ prefix.

### Current Interceptors

 * **Code** - A C# code block that performs any action.  It has access to RenderContext, Params, EmitTokens and Emit() as globals.
 * **Component** - Render a component by its html name, eg `<Component name="components/twoup.c.html"></Component>`
 * **Define** - Define a variable by *name* and *value*.  Also supports *if-undefined* so you can set it as a default value.
 * **ForEach** - Loop for each *name* in the *in* collection.
 * **Include** - Include an html template at this location.  `<Include name="include/includeme.t.html"/>`
 * **Json** - Load an object from a json file. <Json name="variableName" from="data/file.json"/>
 * **Markdown** - Load markdown from a content file, and render it as html. `<Markdown name="md/markdown.t.md"/>`
 * **OnLink** - Set variables for the parameters if we are on the page referred to by the uri parameter. `<OnLink uri="/" setVariable="atHome">$(_uri)</OnLink>`
 * **Picture** - Render an image using a picture tag, with srcset's for multiple resolutions.  The image is resized automatically and stored in the rendered output.  `<Picture src="/static/image.jpg"/>`. Src is the published path, rather than the site input path.
 * **Require** - Require that a variable be set, throw a build error if it isnt.


## Site Configuration
Site configuration can be Yaml (site.yaml) or Json (site.json) at the root of your site folders;

```yaml
# Url to your site, can be used anywhere as the base of a URL to your site.
uriName: https://yousite.uri

# The name of the theme to use, it must be a submodule in the "themes" folder, change this folder through build configuratrion.
theme: themeName
# Items to copy from your site folder, either static or generated items.
copy:
  -
    from: a folder, relative to your site folder
    glob: a glob that specifies the files to copy
    to: a folder in the published output
    
# Files to be considered as content, a processor for the extension must also exist.  Below is the default.
pageGlobs:
  - **/*.html
  - **/*.md
  
# Globs not to be considered as content pages, below is the default
excludeGlobs:
  - node_modules/**/*
  - **/*.t.*
  - **/*.c.*

# Variables are available via the variable syntax to all pages.
variables
  -
    name: siteName
    value: Verge Start

# Derived files that you dont want to watch when you are serving the site locally, this is an example
derived:
  - static/js/**/*.js

# Executables to run at Install (initial start), Pre build or Post build, use to install npm packages, call npm run build, or anything
exec:
  install:
    -
      in: ./
      cmd: npm install
  prebuild:
    -
      in: ./
      cmd: npm run build
  postbuild:
    -
      in:
      cmd:

# The site default language
language: en
```

## Theme Configuration
Configuration for a theme, speficied in Yaml (theme.yaml) or Json (theme.json), at the root of the theme.
```yaml
# Name of the theme
name: patternfly

# Copy files.  See site configuration, copies are taken from the path relative to the theme, and copied into the published output
copy:
- 
    from: node_modules/@patternfly/patternfly/
    glob: patternfly.min.css
    to: theme/patternfly/css/

# Executions, see site configuration, executable folders are relative to the theme folder.
exec:
```

## Build Process
```yaml
# A list of sites that are built
sitePaths:
  - 
    path: site
    publish: publish
    
# The path used to store themes by their theme name
themesPath: themes

# When content is not found in the site, a list of paths to fall through to find content
# You could create your own component library and have it checked for components, as an example, by adding it to fallbackPaths above the theme and library
fallbackPaths:
  - $(themesPath)/$(themeName)
  - lib

```

## Your own Interceptors, Processors, Generators

To inject your own items, make a new command line project, inject them into a services collection, then call verge.  It will still handle the build and serve command line arguments, but with your additional items.

```c#
// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Outrage.Verge.Host;  // <- You will need a dependency on this project

IServiceCollection services = new ServiceCollection();
services.AddSingleton<IInterceptor, YourInterceptor>();
services.AddSingleton<IProcessorFactory, YourProcessorFactory>();
services.AddSingleton<IContentGenerator, YourGenerator>();
await VergeExecutor.Start(args, services);
```

### Interceptors

C# classes that inherit from IInterceptor and produce html or tokens according to your rules.

See IInterceptor and ForEachInterceptor in the verge repository.
Inject your own interceptors via dependency injection into the service collection:
```c#
services.AddSingleton<IInterceptor, YourInterceptor>();
```

### Processors

C# classes that know how to process a content type with a given extension.  You must implement three classes, a Factory, a Processor and a Writer.
The factory is the creator of the other two, a processor interprets a format by its extension, and produces HTML output into sections, and the writer knows how to structure the output as HTML.

See MarkdownProcessorFactory, MarkdownProcessor and MarkdownWriter as examples.
Inject your own procesors via dependency injection into the service collection:
```c#
services.AddSingleton<IProcessorFactory, YourProcessorFactory>();
```

### Generators

A generator is notified of pages being created, and then finalizes itself at the end of the build process.
The only generator at the moment is the sitemap generator, SitemapGenerator.
Inject your own generators via dependency injection into the service collection:
```c#
services.AddSingleton<IContentGenerator, YourGenerator>();
```

Welcome to the Edge.
