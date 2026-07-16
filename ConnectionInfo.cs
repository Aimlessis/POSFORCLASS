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
            ConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? throw new InvalidOperationException("DATABASE_URL no configurada");
        }
    }
}
