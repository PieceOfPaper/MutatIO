using System;
using System.Buffers.Binary;

namespace MutatIO.Shared.Protocol
{
    /// <summary>
    /// 메시지 바이트 인코딩입니다.
    /// 임시: 직렬화 라이브러리(MemoryPack 또는 MessagePack-CSharp)가 결정되면 교체합니다.
    /// 형식은 [MessageType 1바이트][내용]이며 숫자는 리틀 엔디언입니다.
    /// </summary>
    public static class MessageCodec
    {
        private const int PingPayloadSize = sizeof(uint) + sizeof(long);
        private const int PingMessageSize = 1 + PingPayloadSize;

        public static bool TryReadType(ReadOnlySpan<byte> message, out MessageType type)
        {
            if (message.IsEmpty)
            {
                type = default;
                return false;
            }

            type = (MessageType)message[0];
            return true;
        }

        public static byte[] EncodePing(PingPayload payload)
        {
            return Encode(MessageType.Ping, payload);
        }

        public static byte[] EncodePong(PingPayload payload)
        {
            return Encode(MessageType.Pong, payload);
        }

        public static bool TryDecodePing(ReadOnlySpan<byte> message, out PingPayload payload)
        {
            return TryDecode(MessageType.Ping, message, out payload);
        }

        public static bool TryDecodePong(ReadOnlySpan<byte> message, out PingPayload payload)
        {
            return TryDecode(MessageType.Pong, message, out payload);
        }

        private static byte[] Encode(MessageType type, PingPayload payload)
        {
            var buffer = new byte[PingMessageSize];
            buffer[0] = (byte)type;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(1), payload.Sequence);
            BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(1 + sizeof(uint)), payload.ClientTimestamp);
            return buffer;
        }

        private static bool TryDecode(MessageType expectedType, ReadOnlySpan<byte> message, out PingPayload payload)
        {
            if (message.Length != PingMessageSize || message[0] != (byte)expectedType)
            {
                payload = default;
                return false;
            }

            var sequence = BinaryPrimitives.ReadUInt32LittleEndian(message.Slice(1));
            var timestamp = BinaryPrimitives.ReadInt64LittleEndian(message.Slice(1 + sizeof(uint)));
            payload = new PingPayload(sequence, timestamp);
            return true;
        }
    }
}
