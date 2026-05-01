using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.VpetAPI
{
    internal sealed class MenuManager
    {
        private sealed class MenuEntryRef
        {
            public MenuItem Parent { get; }
            public MenuItem Item { get; }
            public string CallbackUrl { get; }

            public MenuEntryRef(MenuItem parent, MenuItem item, string callbackUrl)
            {
                Parent = parent;
                Item = item;
                CallbackUrl = callbackUrl;
            }
        }

        private static readonly HttpClient callbackHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        private readonly IMainWindow mw;
        private readonly Dictionary<string, MenuItem> rootMenus = new Dictionary<string, MenuItem>(StringComparer.Ordinal);
        private readonly Dictionary<string, MenuEntryRef> entryCache = new Dictionary<string, MenuEntryRef>(StringComparer.Ordinal);

        public MenuManager(IMainWindow mw)
        {
            this.mw = mw ?? throw new ArgumentNullException(nameof(mw));
        }

        public void Apply(Dictionary<string, Dictionary<string, SetMenuItemRequest>> request)
        {
            if (request == null || request.Count == 0)
                return;

            foreach (var rootPair in request)
            {
                var rootName = rootPair.Key?.Trim();
                if (string.IsNullOrEmpty(rootName))
                    continue;

                var rootSpec = rootPair.Value;
                if (rootSpec == null || rootSpec.Count == 0)
                    continue;

                var rootMenu = GetOrCreateRootMenu(rootName);
                ClearRoot(rootName, rootMenu);

                foreach (var itemPair in rootSpec)
                {
                    var menuKey = itemPair.Key?.Trim();
                    if (string.IsNullOrEmpty(menuKey))
                        continue;

                    var itemSpec = itemPair.Value;
                    var name = itemSpec?.Name?.Trim();
                    var callbackUrl = itemSpec?.CallbackUrl?.Trim();
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(callbackUrl))
                        continue;

                    var segments = SplitPathSegments(name);
                    if (segments.Count == 0)
                        continue;

                    var parent = rootMenu;
                    for (int i = 0; i < segments.Count - 1; i++)
                    {
                        parent = GetOrCreateChildMenu(parent, segments[i]);
                    }

                    var leafHeader = segments[segments.Count - 1];
                    var leaf = new MenuItem
                    {
                        Header = leafHeader,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                    };

                    leaf.Click += (_, _) =>
                    {
                        _ = TryInvokeCallbackAsync(callbackUrl);
                    };

                    var cacheKey = MakeEntryKey(rootName, menuKey);
                    parent.Items.Add(leaf);
                    entryCache[cacheKey] = new MenuEntryRef(parent, leaf, callbackUrl);
                }
            }
        }

        private void ClearRoot(string rootName, MenuItem rootMenu)
        {
            var prefix = rootName + "\u001F";
            List<string>? keysToRemove = null;
            foreach (var kv in entryCache)
            {
                if (!kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                keysToRemove ??= new List<string>();
                keysToRemove.Add(kv.Key);
            }

            if (keysToRemove != null)
            {
                for (int i = 0; i < keysToRemove.Count; i++)
                {
                    entryCache.Remove(keysToRemove[i]);
                }
            }

            rootMenu.Items.Clear();
        }

        private MenuItem GetOrCreateRootMenu(string rootName)
        {
            if (rootMenus.TryGetValue(rootName, out var cached))
                return cached;

            var menuDiy = mw.Main?.ToolBar?.MenuDIY;
            if (menuDiy == null)
                throw new InvalidOperationException("MenuDIY 不可用");

            for (int i = 0; i < menuDiy.Items.Count; i++)
            {
                if (menuDiy.Items[i] is MenuItem mi && string.Equals(mi.Header?.ToString(), rootName, StringComparison.Ordinal))
                {
                    rootMenus[rootName] = mi;
                    return mi;
                }
            }

            var root = new MenuItem
            {
                Header = rootName,
                HorizontalContentAlignment = HorizontalAlignment.Center,
            };
            menuDiy.Items.Add(root);
            rootMenus[rootName] = root;
            return root;
        }

        private static MenuItem GetOrCreateChildMenu(MenuItem parent, string header)
        {
            for (int i = 0; i < parent.Items.Count; i++)
            {
                if (parent.Items[i] is MenuItem mi && string.Equals(mi.Header?.ToString(), header, StringComparison.Ordinal))
                    return mi;
            }

            var created = new MenuItem
            {
                Header = header,
                HorizontalContentAlignment = HorizontalAlignment.Center,
            };
            parent.Items.Add(created);
            return created;
        }

        private static string MakeEntryKey(string rootName, string menuKey) => rootName + "\u001F" + menuKey;

        private static List<string> SplitPathSegments(string name)
        {
            var raw = name.Split(new[] { "->" }, StringSplitOptions.None);
            var list = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                var s = raw[i]?.Trim();
                if (!string.IsNullOrEmpty(s))
                    list.Add(s);
            }
            return list;
        }

        private static async Task TryInvokeCallbackAsync(string callbackUrl)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, callbackUrl);
                using var _ = await callbackHttp.SendAsync(req).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }
}
