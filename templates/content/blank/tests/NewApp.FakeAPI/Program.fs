open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

let builder = WebApplication.CreateBuilder ()
builder.WebHost.UseUrls ("http://0.0.0.0:80") |> ignore

let app = builder.Build ()

app.MapGet (
    "/healthcheck",
    (fun (context: HttpContext) ->
        app.Logger.Log (LogLevel.Information, "Received a request at '/healthcheck' endpoint")
        context.Response.WriteAsync ("OK"))
)
|> ignore

app.Run ()
