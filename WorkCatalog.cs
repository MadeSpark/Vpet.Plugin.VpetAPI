using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.VpetAPI
{
    public sealed class WorkCatalog
    {
        private readonly IMainWindow mw;

        public WorkCatalog(IMainWindow mw)
        {
            this.mw = mw ?? throw new ArgumentNullException(nameof(mw));
        }

        public async Task<IReadOnlyList<string>> GetNameListAsync(WorkCategory category, CancellationToken token)
        {
            var list = await GetListAsync(category, token).ConfigureAwait(false);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (var item in list)
            {
                var name = GetDisplayName(item).Trim();
                if (name.Length == 0)
                    continue;
                if (seen.Add(name))
                    result.Add(name);
            }
            return result;
        }

        public async Task<bool> TryStartAsync(WorkCategory category, string? id, CancellationToken token, LevelLimitAdjuster? adjuster = null)
        {
            id ??= string.Empty;
            id = id.Trim();

            var list = await GetListAsync(category, token).ConfigureAwait(false);
            if (list.Count == 0)
                return false;

            object? item = null;
            if (string.IsNullOrEmpty(id))
            {
                var index = Random.Shared.Next(0, list.Count);
                item = list[index];
            }
            else if (int.TryParse(id, out var index))
            {
                if (index >= 0 && index < list.Count)
                    item = list[index];
            }
            else
            {
                item = list.FirstOrDefault(w => string.Equals(GetDisplayName(w), id, StringComparison.OrdinalIgnoreCase));
            }

            if (item == null)
                return false;

            // 如果提供了等级限制调整器，先调整等级限制
            if (adjuster != null && item is GraphHelper.Work work)
            {
                item = adjuster.AdjustBeforeStart(work);
            }

            await mw.Dispatcher.InvokeAsync(() => InvokeStartWork(item));
            return true;
        }

        private async Task<List<object>> GetListAsync(WorkCategory category, CancellationToken token)
        {
            object? ws = null;
            object? ss = null;
            object? ps = null;

            await mw.Dispatcher.InvokeAsync(() =>
            {
                var m = mw.Main.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(mi =>
                    {
                        if (!string.Equals(mi.Name, "WorkList", StringComparison.OrdinalIgnoreCase))
                            return false;
                        var ps = mi.GetParameters();
                        return ps.Length == 3 && ps.All(p => p.IsOut);
                    });
                if (m == null)
                    return;

                var args = new object?[] { null, null, null };
                m.Invoke(mw.Main, args);
                ws = args[0];
                ss = args[1];
                ps = args[2];
            });

            var src = category switch
            {
                WorkCategory.Work => ws,
                WorkCategory.Study => ss,
                WorkCategory.Play => ps,
                _ => ws
            };

            return ToObjectList(src);
        }

        private void InvokeStartWork(object item)
        {
            var methods = mw.Main.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var m in methods)
            {
                if (!string.Equals(m.Name, "StartWork", StringComparison.OrdinalIgnoreCase))
                    continue;

                var ps = m.GetParameters();
                if (ps.Length != 1)
                    continue;

                if (!ps[0].ParameterType.IsAssignableFrom(item.GetType()))
                    continue;

                m.Invoke(mw.Main, new[] { item });
                return;
            }

            var fallback = mw.Main.GetType().GetMethod("StartWork", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fallback?.Invoke(mw.Main, new[] { item });
        }

        private static List<object> ToObjectList(object? listObj)
        {
            if (listObj == null)
                return new List<object>();

            if (listObj is IList ilist)
                return ilist.Cast<object>().ToList();

            if (listObj is IEnumerable enumerable)
                return enumerable.Cast<object>().ToList();

            return new List<object>();
        }

        private static string GetDisplayName(object work)
        {
            var t = work.GetType();
            var names = new[] { "Name", "Title", "DisplayName", "WorkName" };
            foreach (var n in names)
            {
                var prop = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public);
                if (prop?.PropertyType == typeof(string))
                {
                    var v = prop.GetValue(work) as string;
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }

            return work.ToString() ?? string.Empty;
        }
    }
}
