
using Starcounter.Internal;
using System;
using System.Linq;

namespace Starcounter {
    /// <summary>
    /// Defines constants and methods relating to configuration of
    /// commit hooks in the kernel.
    /// </summary>
    internal static class CommitHookConfiguration {
        /// <summary>
        /// Internal token/flag representing inserts.
        /// </summary>
        internal const uint Insert = sccoredb.CommitHookConfigInsert;
        /// <summary>
        /// Internal token/flag representing updates.
        /// </summary>
        internal const uint Update = sccoredb.CommitHookConfigUpdate;
        /// <summary>
        /// Internal token/flag representing deletes.
        /// </summary>
        internal const uint Delete = sccoredb.CommitHookConfigDelete;


        /// <summary>
        /// Gets the <see cref="CommitHookConfiguration"/> corresponding to
        /// the given <see cref="HookType"/>.
        /// </summary>
        /// <param name="type">The configuration whose
        /// corresponding type of hook to return.</param>
        /// <returns>Hook configuration corresponding to the given type.</returns>
        internal static uint FromHookType(uint type) {
            uint configuration = 0;
            switch (type) {
                case HookType.CommitInsert:
                    configuration = CommitHookConfiguration.Insert;
                    break;
                case HookType.CommitUpdate:
                    configuration = CommitHookConfiguration.Update;
                    break;
                case HookType.CommitDelete:
                    configuration = CommitHookConfiguration.Delete;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        string.Format("Hook type {0} can not be mapped to a commit hook configuration", type));
            };
            return configuration;
        }

        /// <summary>
        /// Gets the <see cref="HookType"/> corresponding to the given
        /// <see cref="CommitHookConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The configuration whose
        /// corresponding type of hook to return.</param>
        /// <returns>Hook type corresponding to the given config.</returns>
        internal static uint ToHookType(uint configuration) {
            uint type = 0;
            switch (configuration) {
                case CommitHookConfiguration.Insert:
                    type = HookType.CommitInsert;
                    break;
                case CommitHookConfiguration.Update:
                    type = HookType.CommitUpdate;
                    break;
                case CommitHookConfiguration.Delete:
                    type = HookType.CommitDelete;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        string.Format("Commit hook configuration {0} can not be mapped to a type", configuration));
            };
            return type;
        }

        /// <summary>
        /// Returns a mask with all configuration installed for
        /// the type given a <see cref="HookKey"/>.
        /// </summary>
        /// <param name="key">Key whose type we should gather installed
        /// configuration for.</param>
        /// <returns>Mask with all hook configuration installed for the
        /// given type.
        /// </returns>
        internal static uint CalculateEffectiveConfiguration(HookKey key) {
            uint result = 0;
            foreach (var installedKey in InvokableHook.HooksPerTrigger.Keys.Where(k => HookType.IsCommitHook(k.TypeOfHook))) {
                if (installedKey.TypeId == key.TypeId) {
                    result |= CommitHookConfiguration.FromHookType(installedKey.TypeOfHook);
                }
            }
            return result;
        }
    }
}
