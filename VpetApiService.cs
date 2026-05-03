using System;

namespace VPet.Plugin.VpetAPI
{
    /// <summary>
    /// VpetAPI 依赖服务
    /// 供其他 Mod 通过反射调用，判断 VpetAPI 是否成功加载
    /// </summary>
    public static class VpetApiService
    {
        private static readonly object Sync = new();
        private static bool isLoaded = false;
        private static bool isRunning = false;
        private static int port = 52814;
        private static string? errorMessage = null;

        /// <summary>
        /// VpetAPI 插件是否已加载（无论 HTTP 服务是否成功启动）
        /// </summary>
        public static bool IsLoaded
        {
            get
            {
                lock (Sync)
                {
                    return isLoaded;
                }
            }
        }

        /// <summary>
        /// VpetAPI HTTP 服务是否正在运行
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                lock (Sync)
                {
                    return isRunning;
                }
            }
        }

        /// <summary>
        /// HTTP 服务端口号
        /// </summary>
        public static int Port
        {
            get
            {
                lock (Sync)
                {
                    return port;
                }
            }
        }

        /// <summary>
        /// HTTP 服务地址
        /// </summary>
        public static string ServiceUrl => $"http://127.0.0.1:{Port}/";

        /// <summary>
        /// 如果服务启动失败，这里包含错误信息
        /// </summary>
        public static string? ErrorMessage
        {
            get
            {
                lock (Sync)
                {
                    return errorMessage;
                }
            }
        }

        /// <summary>
        /// 标记插件已加载（由插件内部调用）
        /// </summary>
        internal static void MarkLoaded()
        {
            lock (Sync)
            {
                isLoaded = true;
            }
        }

        /// <summary>
        /// 启动服务（由插件内部调用）
        /// </summary>
        internal static void Start(int servicePort)
        {
            lock (Sync)
            {
                isRunning = true;
                port = servicePort;
                errorMessage = null;
            }
        }

        /// <summary>
        /// 标记服务启动失败（由插件内部调用）
        /// </summary>
        internal static void MarkFailed(string error)
        {
            lock (Sync)
            {
                isRunning = false;
                errorMessage = error;
            }
        }

        /// <summary>
        /// 停止服务（由插件内部调用）
        /// </summary>
        internal static void Stop()
        {
            lock (Sync)
            {
                isRunning = false;
            }
        }
    }
}
