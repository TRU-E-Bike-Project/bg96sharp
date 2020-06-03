using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BG96Sharp
{
    public delegate void MqttMessageReceivedHandler(object sender, MqttMessageReceivedEventArgs e);

    public interface IMqttClient
    {
        event MqttMessageReceivedHandler MessageReceived;
        event EventHandler Connected;

        Task<bool> SendMqttMessageAsync(string topic, string data);
        Task<bool> SendMqttMessageAsync(string topic, byte[] data);
        Task<bool> SendMqttMessageAsync(string topic, byte[] data, CancellationToken cancellationToken);

        Task<bool> SubscribeToTopicAsync(string topic);

        Task<bool> ConnectAsync();
        Task<bool> CloseAsync();

        bool IsConnected { get; }
    }

    public class MqttMessageReceivedEventArgs : EventArgs
    {
        public string Topic { get; set; }
        public byte[] Content { get; set; }
        public string Message { get => Encoding.Unicode.GetString(Content, 0, Content.Length); }
    }
}
