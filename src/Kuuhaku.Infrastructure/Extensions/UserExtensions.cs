using System;
using Discord;
using Discord.WebSocket;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class UserExtensions
    {
        private static (ImageFormat format, UInt16 size) GetAvatarFormat(this IUser user, UInt16 maxSize)
        {
            var format = user.AvatarId.StartsWith("a_")
                ? ImageFormat.Gif
                : ImageFormat.WebP;
            var size = (UInt16) Math.Max(maxSize, format == ImageFormat.Gif ? 128 : 512);
            return (format, size);
        }

        private static String GetAvatarUrl(this IUser user, (ImageFormat format, UInt16 size) tuple)
            => user?.GetAvatarUrl(tuple.format, tuple.size);

        public static String GetAvatar(this IUser user, UInt16 maxSize = 512)
        {
            return user.AvatarId.IsEmpty()
                ? user.GetDefaultAvatarUrl()
                : user.GetAvatarUrl(user.GetAvatarFormat(maxSize));
        }

        public static String GetName(this IUser user)
        {
            if (user is SocketGuildUser guildUser && !guildUser.Nickname.IsEmpty())
                return guildUser.Nickname;
            return user.Username;
        }
    }
}
