using Puppet.Tools;
using Puppet.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Puppet
{
    public static class PuppetContextExtensions
    {
        /// <summary>
        /// Used to parse and accept input of any type, returns fallBack if cannot parse (used for defaults). Used like:
        /// int x = await ctx.RequestAsync("Enter Number:", s => (int.TryParse(s, out int v), v), 0);
        /// </summary>
        /// <example>
        /// int x = await ctx.RequestAsync("Enter Number:", s => (int.TryParse(s, out int v), v), 0);
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctx"></param>
        /// <param name="prompt"></param>
        /// <param name="parser"></param>
        /// <param name="fallBack"></param>
        /// <returns></returns>
        /// <exception cref="PuppetUserException"></exception>
        public static async Task<T> RequestAsync<T>(this PuppetContext ctx, string prompt, Func<string, (bool success, T Value)> parser, T? fallBack = default)
        {
            string input = await ctx.ReadLineAsync(prompt);
            var result = parser(input);
            if (result.success) return result.Value;
            if (fallBack is not null) return fallBack;
            throw new PuppetUserException($"Cannot parse '{input}'");
        }

        /// <summary>
        /// Used to parse and accept input of any type, returns fallback if input is among specified defaultStrings. If no default strings are specified, is is equal to [" ", "Default", "Fallback"].
        /// Used like:
        /// int x = ctx.RequestAsync(
        ///     "Enter Number:",
        ///     s => (int.TryParse(s, out int v), v),
        ///     0, " ", "Default", "FallBack", "Zero", No"
        ///     );
        /// </summary>
        /// <example>
        /// int x = ctx.RequestAsync(
        ///     "Enter Number:",
        ///     s => (int.TryParse(s, out int v), v),
        ///     0, " ", "Default", "FallBack", "Zero", No"
        ///     );
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctx"></param>
        /// <param name="prompt"></param>
        /// <param name="parser"></param>
        /// <param name="fallBack"></param>
        /// <param name="defaultStrings"></param>
        /// <returns></returns>
        /// <exception cref="PuppetUserException"></exception>
        public static async Task<T> RequestAsync<T>(this PuppetContext ctx, string prompt, Func<string, (bool success, T Value)> parser, T fallBack, params string[] defaultStrings)
        {
            string input = await ctx.ReadLineAsync(prompt);
            if (defaultStrings.Any(s => string.Equals(s, input, StringComparison.OrdinalIgnoreCase))) return fallBack;
            var result = parser(input);
            if (result.success) return result.Value;
            throw new PuppetUserException($"Cannot parse '{input}'");
        }

        /// <summary>
        /// Used to parse and accept input of any type. If cannot parse, will ask again. Used like:
        /// int x = await ctx.RequireAsync(
        ///     "Enter Number:", 
        ///     s => (int.TryParse(s, out int v), v),
        ///     "Could not parse, please try again." 
        ///     );
        /// </summary>
        /// <example>
        /// int x = await ctx.RequireAsync(
        ///     "Enter Number:", 
        ///     s => (int.TryParse(s, out int v), v),
        ///     "Could not parse, please try again." 
        ///     );
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctx"></param>
        /// <param name="prompt"></param>
        /// <param name="retryPrompt"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        public static async Task<T> RequireAsync<T>( this PuppetContext ctx, string prompt, Func<string, (bool success, T Value)> parser, string retryPrompt)
        {
            while (true)
            {
                try { return await ctx.RequestAsync(prompt, parser); }
                catch (PuppetUserException) { ctx.WriteLine(retryPrompt); }
            }
        }

        /// <summary>
        /// Used to parse and accept input of any type. If cannot parse, will ask again. If Input is one of the default strings, will return fallback. If no default strings are specified, it is equal to [" ", "Default", "Fallback"].
        /// Used like:
        /// int x = await ctx.RequireAsync(
        ///     "Enter Number:", 
        ///     s => (int.TryParse(s, out int v), v),
        ///     "Could not parse, please try again.",
        ///     0, " ", "Default", "FallBack", "Zero", No"
        ///     );
        /// </summary>
        /// <example>
        /// int x = await ctx.RequireAsync(
        ///     "Enter Number:", 
        ///     s => (int.TryParse(s, out int v), v),
        ///     "Could not parse, please try again.",
        ///     0, " ", "Default", "FallBack", "Zero", No"
        ///     );
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctx"></param>
        /// <param name="prompt"></param>
        /// <param name="parser"></param>
        /// <param name="retryPrompt"></param>
        /// <param name="fallBack"></param>
        /// <param name="defaultStrings"></param>
        /// <returns></returns>
        public static async Task<T> RequireAsync<T>(this PuppetContext ctx, string prompt, Func<string, (bool success, T Value)> parser, string retryPrompt, T fallBack, params string[] defaultStrings
            )
        {
            if (defaultStrings.Length == 0) defaultStrings = [" ", "default", "fallback"];
            while (true)
            {
                try { return await ctx.RequestAsync(prompt, parser, fallBack, defaultStrings); }
                catch (PuppetUserException) { ctx.WriteLine(retryPrompt); }
            }
        }

        /// <summary>
        /// Used for required strings. Will only return if given string is not null or whitespace.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="prompt">What user is presented with.</param>
        /// <param name="retryPrompt">What user is presented with if input is null or white space.</param>
        /// <returns></returns>
        public static async Task<string> RequireString(this PuppetContext ctx, string prompt, string retryPrompt)
        {
            while (true)
            {
                string? input = await ctx.ReadLineAsync(prompt);
                if (!string.IsNullOrWhiteSpace(input)) return input;
                else ctx.WriteLine(retryPrompt);
            }
        }

        /// <summary>
        /// Used for optional strings. If input is null or whitespace, will return null.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="prompt">What user is presented with.</param>
        /// <returns></returns>
        public static async Task<string?> RequestStringNullable(this PuppetContext ctx, string prompt)
        {
            string? input = await ctx.ReadLineAsync(prompt);
            if (string.IsNullOrWhiteSpace(input)) return null;
            else return input;
        }
        /// <summary>
        /// Finds a command in dictionary. If it is left blank, returns every command. If it cannot be found in dictionary, will return findings in Aliases.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        public static List<PuppetCommand> SearchDictionary(this PuppetContext ctx, string searchTerm = "")
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return ctx.CommandIndex.Values.ToList();
            List<PuppetCommand> filtered = ctx.CommandIndex.Values.Where(c => c.Address.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            if (filtered.Count == 0)
                filtered = ctx.AliasIndex
                    .Where(kv => kv.Key.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Value).Distinct().ToList();
            filtered.OrderBy(c => c.Address);
            return filtered;
        }

        public static async Task<bool> ConfirmAsync(this PuppetContext ctx, string prompt = $"(Y/N):", bool? fallBack = null) => (await ctx.ReadLineAsync(prompt)).ParseConfirmation(fallBack);

        public static async Task<bool> ConfirmRequireAsync(this PuppetContext ctx, string prompt = $"(Y/N)", string retryPrompt = "Could not parse, try again.")
        {
            while (true)
            {
                try { return await ctx.ConfirmAsync(prompt); }
                catch { ctx.WriteLine(retryPrompt); }
            }
        }
    }
}
