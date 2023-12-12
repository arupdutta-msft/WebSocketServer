using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// setup web socket for stream in
app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            try
            {
                string partialData = "";
                while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent)
                {
                    byte[] receiveBuffer = new byte[2048];
                    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                    if (receiveResult.MessageType != WebSocketMessageType.Close)
                    {
                        string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');

                        try
                        {
                            if (receiveResult.EndOfMessage)
                            {
                                data = partialData + data;
                                partialData = "";

                                if (data != null)
                                {

                                    Console.OutputEncoding = System.Text.Encoding.Unicode;
                                    Console.WriteLine($"\n [{DateTime.UtcNow}] {data}");
                                }
                            }
                            else
                            {
                                partialData = partialData + data;
                            }
                        }
                        catch (Exception ex)
                        { Console.WriteLine($"Exception 1 -> {ex}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception 2 -> {ex}");
            }
            finally
            {

            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});

app.UseAuthorization();

app.MapControllers();
app.UseHttpsRedirection();
app.Run();