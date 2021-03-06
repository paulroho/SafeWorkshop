module Server

open Saturn
open Config
open Microsoft.Extensions.DependencyInjection
open Giraffe.Serialization
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder.Internal

let publicPath = Path.GetFullPath "../Client/public"

let port = 8085us

let endpointPipe = pipeline {
    plug head
    plug requestId
}

let configureSerialization (services:IServiceCollection) =
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

let app = application {
    pipe_through endpointPipe

    error_handler (fun ex _ -> pipeline { render_html (InternalError.layout ex) })
    use_router Router.appRouter
    url ("http://0.0.0.0:" + port.ToString() + "/")
    memory_cache
    use_static publicPath
    service_config configureSerialization
    use_cookies_authentication_with_config (fun opts ->
        opts.LoginPath <- PathString "/login"
        opts.LogoutPath <- PathString "/logout"

        ()
    )
    logging configureLogging
    use_gzip
    use_config (fun _ -> {connectionString = "DataSource=database.sqlite"} ) //TODO: Set development time configuration
}



[<EntryPoint>]
let main _ =
    printfn "Working directory - %s" (System.IO.Directory.GetCurrentDirectory())
    run app
    0 // return an integer exit code