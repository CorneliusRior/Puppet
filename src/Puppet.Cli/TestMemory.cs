using System;
using System.Collections.Generic;
using System.Text;

namespace Puppet.Cli
{
    public class TestMemory
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public double MaxDeadLift { get; set; }
        public string Motto { get; set; }

        public TestMemory(string name, int age, DateTime dateOfBirth, double maxDeadLift, string motto)
        {
            Name = name;
            Age = age;
            DateOfBirth = dateOfBirth;
            MaxDeadLift = maxDeadLift;
            Motto = motto;
        }

        public string Print() => $"This guy's name is {Name}, he is {Age} years old, and his birthday is {DateOfBirth.ToString("M")}. The most he has ever deadlifted is {MaxDeadLift}kg, and he lives by the motto: \"{Motto}\"";
    }
}
