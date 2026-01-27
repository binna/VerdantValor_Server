using System.Text;
using System.Text.Json;
using Common.Base;
using Common.Helpers;
using VerdantValorShared.Common.Web;
using VerdantValorShared.DTOs.Web;

namespace WebServer.Pipeline;

public class DecryptReqMiddleware
{
    private readonly RequestDelegate mNext;
    
    public DecryptReqMiddleware(RequestDelegate next)
    {
        mNext = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            context.Request.EnableBuffering();

            string cipherText;
            using (var reader = new StreamReader(
                       context.Request.Body,
                       Encoding.UTF8,
                       detectEncodingFromByteOrderMarks: false,
                       bufferSize: 1024,
                       leaveOpen: true))
            {
                cipherText = await reader.ReadToEndAsync();
            }
        
            context.Request.Body.Position = 0;

            if (string.IsNullOrEmpty(cipherText))
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsJsonAsync(
                    ApiResponse.From(
                        AppEnum.EResponseStatus.FailDecrypt, AppEnum.ELanguage.En));
                return;
            }

            var encryptReq = JsonSerializer.Deserialize<EncryptReq>(cipherText);

            if (encryptReq == null)
            {
                await context.Response.WriteAsJsonAsync(
                    ApiResponse.From(AppEnum.EResponseStatus.FailDecrypt, AppEnum.ELanguage.En));
                return;
            }

            var request = SecurityHelper.DecryptPayloadToBytes(encryptReq);
            var newBody = new MemoryStream(request);

            context.Response.Body = newBody;
            context.Request.ContentLength  = request.Length;
            context.Request.Body.Position = 0;
        }

        await mNext(context);
    }
}