using DotNetEnv;
using System;
namespace DolcePOSDummies
{

    public static class ConnectionInfo
    {
        public static readonly string ConnectionString;

        static ConnectionInfo()
        {
            Env.Load();

            var cs = Environment.GetEnvironmentVariable("DATABASE_URL");

            ConnectionString = cs
                ?? throw new InvalidOperationException("DATABASE_URL no configurada");
        }
    }
}
