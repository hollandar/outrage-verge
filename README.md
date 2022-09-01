# Outrage.Verge

A static site generator, for life on the edge.

Generates a static site from HTML templates only, there will not be code in the templates, just HTML, you can instead create interceptors that can handle certain tags and augment the output in any way you like.

An interceptor takes the attributes and the inner content from a tag, and allows you to transform it in any way you want.

Because interceptors are injected via dependency injection, you can add whatever custom tags as interceptors; using whatever data is available to you; independent of the core publishing infrastructure.

Verge already supports:
 * HTML pages
 * HTML templates
 * HTML themes
 * Interceptors

In the future, Verge will support:
 * Markdown content
 * Static files
 * Image transformations
 * Css and Js minimization
 * Copies of static content, to pull from npm etc.
 
The command line already handles generation and serving and monitoring for changed content for regeneration.

We expect that your repo will pull in outrage-verge and you will create your own command line, injecting your own services.
That way, your additions are yours alone, your code is typesafe, and execution is fast.

Your process will be:
 1. Create a git repo for your site
 2. Add outrage-verge as a submodule.
 3. Create your own command line project that calls `VergeExecutor.Start(args, serviceCollection)`
 4. Integrate your own interceptors or pull any of ours in
 5. Build a script that runs your command using dotnet run Your.Cmd build --in infolder --out outfolder
 6. Push the published site to the edge using github actions or ci/cd, edge host it on netlify, amazon s3, or whatever.
 
Interested??
 
 
