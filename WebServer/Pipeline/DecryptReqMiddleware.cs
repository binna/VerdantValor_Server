using System.Text;
using System.Text.Json;
using SharedLibrary.Common;
using SharedLibrary.Helpers;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Protocol.DTOs;

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
                    new ApiResponse(
                        ResponseStatus.FromResponseStatus(
                            EResponseStatus.FailDecrypt, AppEnum.ELanguage.Ko)));
                return;
            }

            var encryptReq = JsonSerializer.Deserialize<EncryptReq>(cipherText);

            if (encryptReq == null)
            {
                await context.Response.WriteAsJsonAsync(
                    new ApiResponse(
                        ResponseStatus.FromResponseStatus(
                            EResponseStatus.FailDecrypt, AppEnum.ELanguage.Ko)));
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