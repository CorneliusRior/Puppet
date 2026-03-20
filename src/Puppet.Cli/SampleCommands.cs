using static Puppet.Tools.CmdBuilder;
using System;
using System.Collections.Generic;
using System.Text;
using Puppet.Models;
using Puppet.Tools;

namespace Puppet.Cli
{
    internal class SampleCommands : IPuppetCommandSet
    {
        public IReadOnlyList<PuppetCommand> Commands =>
        [
            new(name: "Assessment",                
                executeAsync: ConfirmAsync,
                testAsync: ConfirmTestAsync,
                description: "Asks for your assessment on a number of topics."               
            ),
            Cmd("TestJson")
                .ExecJson<TestPayload>(TestJsonAsync)
                .TestJson<TestPayload>(TestTestJsonAsync)            
            .Build(),

            Cmd("ToBox").Exec(ToBox)
                .Children(
                Cmd("Double").Exec(ToDoubleBox).Build())
            .Build(),

            Cmd("TextAnimations").Exec(WaitAnimations).Aliases("ta", "wait").Build(),

            Cmd("ViewFileSample").Exec(ViewFileSample).Build()
        ];

        private async Task ViewFileSample(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string path = args.String(0, "Path");
            string[] lines = File.ReadAllLines(path);
            foreach (string l in lines)
            {
                ctx.WriteStatusSample(l, 25);
                await Task.Delay(25);
            }
            ctx.ClearStatus("Done");
        }

        private async Task WaitAnimations(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int type = args.IntOr(0, "Type", 0);
            string pre = args.StringOr(1, "Prefix", "Loading");
            string suf = args.StringOr(2, "Suffix", "");
            string fin = args.StringOr(3, "Finish", "");
            int waitTime = args.IntOr(4, "Wait Time", 100);
            double seconds = args.DoubleOr(5, "Seconds", 5);

            ctx.WriteLine("Animating:\n");

            WaitAnimation animation = type switch
            {
                1 => WaitAnimation.Spinner,
                2 => WaitAnimation.Elipses,
                3 => WaitAnimation.Bounce,
                4 => WaitAnimation.Road,
                _ => throw new ArgumentOutOfRangeException()
            };
            await ctx.WithWaiterAsync(
                async t =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(seconds));
                },
                pre, suf, fin, waitTime, ct, animation);
            
        }

        private Task ToBox(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string msg = args.String(0, "Msg").Replace("\\n", "\n");
            ctx.WriteLine(msg.ToBox());
            return Task.CompletedTask;
        }

        private Task ToDoubleBox(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string msg = args.String(0, "Msg").Replace("\\n", "\n");
            ctx.WriteLine(msg.ToDoubleBox());
            return Task.CompletedTask;
        }

        private Task TestJsonAsync(PuppetContext ctx, TestPayload pl, CancellationToken ct)
        {
            ctx.WriteLine("Presumably parsed if you're seeing this:");
            ctx.WriteLine($"ID = {pl.Id}, Name = {pl.Name}, Date of Birth = {pl.DateOfBirth.ToString("d")}, Favourite color = {pl.FavouriteColor ?? "No answer"}");
            return Task.CompletedTask;
        }

        private async Task<bool> TestTestJsonAsync(PuppetContext ctx, TestPayload pl, CancellationToken ct)
        {
            if (pl.FavouriteColor is null) return true;
            if (pl.FavouriteColor.Equals("pink", StringComparison.OrdinalIgnoreCase)) return false;
            else return true;
        }

        public sealed record TestPayload(int Id, string Name, DateTime DateOfBirth, string? FavouriteColor);

        private async Task ConfirmAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            bool pizza = await ctx.ConfirmRequireAsync("Do you like pizza?", "Sorry, I didn't hear that.");
            bool hotDogs = await ctx.ConfirmRequireAsync("Do you like hotdogs?", "I can't understand you.");
            bool dinosaurs = await ctx.ConfirmRequireAsync("Do you like dinosaurs", "Too ambiguous, do you or do you not like dinosaurs?");
            double maxDeadLift = await ctx.RequireAsync(
                "What is your max deadlift?",
                s => (double.TryParse(s, out double v), v),
                "Don't be shy, DYEL?",
                -1, "No I don't", "No", "Never", "What does DYEL mean?", "I've never deadlifted", " ", "default", "fallback");

            ctx.WriteLine("Here is your assessment of various things:");
            ctx.WriteLine(pizza ? "You like pizza :)" : "You don't like pizza :(");
            ctx.WriteLine(hotDogs ? "You like hotdogs :)" : "You don't like hotdogs :(");
            ctx.WriteLine(dinosaurs ? "You like dinosaurs :)" : "You don't like dunosaurs :(");
            if (maxDeadLift > 1) ctx.WriteLine($"Your max deadlift is {maxDeadLift}Kg, {(maxDeadLift < 100 ? "DYEL?" : maxDeadLift < 200 ? "Natty" : "Not natty")}");
            else ctx.WriteLine("Doesn't lift.");
            return;
        }

        private async Task<bool> ConfirmTestAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            bool ok = await ctx.RequestAsync(
                "Oh uh, you're testing us? Uhm, I guess, what is your favourite colour?",
                s => (true, !s.Equals("pink", StringComparison.OrdinalIgnoreCase)));
            if (ok)
            {
                ctx.WriteLine("Okay that's cool.");
                return true;
            }
            ctx.WriteLine("Eww pink is a GIRLS color >:(");
            return false;

        }

        
    }
}
