using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using BLL.Helpers;

namespace AutoQuestraderWeb.WebSocketHelpers
{
    public class WebSocketRequestHandler
    {
        private static Dictionary<string, MethodInfo> _methods;
        public static Dictionary<string, MethodInfo> methods
        {
            get
            {

                if (_methods == null)
                {
                    _methods = new Dictionary<string, MethodInfo>();

                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                    foreach (var assembly in assemblies)
                    {
                        var socketMethods = assembly.GetTypes()
                            .SelectMany(t => t.GetMethods())
                            .Where(m => m.GetCustomAttributes(typeof(WebSocketMethodAttribute), false).Length > 0)
                            .ToArray();

                        foreach (var curMethod in socketMethods)
                        {
                            _methods.Add(curMethod.Name, curMethod);
                        }
                    }
                }

                return _methods;
            }
        }

        public static async Task Handle(HttpContext httpContext, WebSocket webSocket)
        {
            /*We define a certain constant which will represent
            size of received data. It is established by us and 
            we can set any value. We know that in this case the size of the sent
            data is very small.
            */
            const int maxMessageSize = 1024;

            //Buffer for received bits.
            var receivedDataBuffer = new ArraySegment<Byte>(new Byte[maxMessageSize]);

            var cancellationToken = new CancellationToken();

            //Checks WebSocket state.
            while (webSocket.State == WebSocketState.Open)
            {
                //Reads data.
                WebSocketReceiveResult webSocketReceiveResult =
                    await webSocket.ReceiveAsync(receivedDataBuffer, cancellationToken);

                //If input frame is cancelation frame, send close command.
                if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        string.Empty, cancellationToken);
                }
                else
                {
                    try
                    {
                        byte[] payloadData = receivedDataBuffer.Array.Where(b => b != 0).ToArray();

                        string receiveString = System.Text.Encoding.UTF8.GetString(payloadData, 0, payloadData.Length);

                        var wrapper = JsonConvert.DeserializeObject<WebSocketDataWrapper>(receiveString);

                        object methodResult = methods[wrapper.methodName].InvokeWithNamedParameters(null, wrapper.parameters);

                        var jsonString = JsonConvert.SerializeObject(methodResult);
                        var bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

                        await webSocket.SendAsync(new ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text, true, cancellationToken);
                    }
                    catch (Exception e) {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(e.Message);

                        await webSocket.SendAsync(new ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text, true, cancellationToken);
                    }
                }
            }
        }
    }
}
