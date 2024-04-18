using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


public class FileParseException : Exception
{
    public readonly uint LineNum;
    public readonly string Line;
    public readonly Exception Cause;

    public override string Message => $"Exception caused by line {LineNum} \"{Line}\". Error message: {Cause.Message}";
    public override string StackTrace => Cause.StackTrace + "\n" + base.StackTrace;

    public FileParseException(uint lineNum, string line, Exception cause)
    {
        LineNum = lineNum;
        Line = line;
        Cause = cause;
    }
}

public class FileParser
{
    public delegate void ParseLineDelegate(string line, string[] args);

    readonly Dictionary<string, ParseLineDelegate> sectionParsers = new();

    public string Line; // The current/last line
    public string[] Args; // The args of the current/last line

    // Section names are case insensitive
    public ParseLineDelegate this[string section]
    {
        get => sectionParsers.GetValueOrDefault(section.ToLower(), null);
        set => sectionParsers[section.ToLower()] = value;
    }

    FileParser RunIfMatch(ICollection<string> argValues, int arg, bool caseSensitive, Action<string> callback)
    {
        foreach (string argValue in argValues)
        {
            if (Match(argValue, arg, caseSensitive) || Match(argValue+":", arg, caseSensitive))
            {
                callback.Invoke(argValue);
                break;
            }
        }
        return this;
    }
    // async Task<FileParser> RunIfMatchAsync(ICollection<string> argValues, int arg, bool caseSensitive, Func<string, Task> callback)
    // {
    //     foreach (string argValue in argValues)
    //     {
    //         if (Match(argValue, arg, caseSensitive) || Match(argValue+":", arg, caseSensitive))
    //         {
    //             await callback.Invoke(argValue);
    //             break;
    //         }
    //     }
    //     return this;
    // }
    public FileParser Switch(Dictionary<string, Action> cases, int arg = 0, bool caseSensitive = false)
        => RunIfMatch(cases.Keys, arg, caseSensitive, argVal => cases[argVal].Invoke());
    // public Task<FileParser> SwitchAsync(Dictionary<string, Func<Task>> cases, int arg = 0, bool caseSensitive = false)
    //     => RunIfMatchAsync(cases.Keys, arg, caseSensitive, async argVal => await cases[argVal].Invoke());

    // public Task<FileParser> KeyValueAsync(Dictionary<string, Func<string, Task>> cases, int arg = 0, bool caseSensitive = false)
    //     => RunIfMatchAsync(cases.Keys, arg, caseSensitive, async argVal =>
    //     {
    //         AssertArgCount(minCount: arg+2);
    //         await cases[argVal].Invoke(GetLineWithoutFirstNArgs(arg+1));
    //     });
    public FileParser KeyValue(Dictionary<string, Action<string>> cases, int arg = 0, bool caseSensitive = false)
        => RunIfMatch(cases.Keys, arg, caseSensitive, argVal =>
        {
            AssertArgCount(minCount: arg+2);
            cases[argVal].Invoke(GetLineWithoutFirstNArgs(arg+1));
        });

    public bool Match(string name, int arg = 0, bool caseSensitive = false)
    {
        if (caseSensitive) return Args[arg] == name;
        else return Args[arg].ToLower() == name.ToLower();
    }

	public void Assert(bool invariant, string message)
	{
		if (!invariant) throw new Exception(message);
	}
	public void AssertArgCount(Func<int, bool> invariant, string expected) => 
		Assert(invariant(Args.Length), $"Expected {expected} arguments, but read {Args.Length} arguments.");
	public void AssertArgCount(int count) => AssertArgCount(c => c == count, $"{count}");
	public void AssertArgCount(int? minCount = null, int? maxCount = null)
	{
		if (maxCount == null && minCount == null) throw new ArgumentException("minCount and maxCount cannot both be null");
		if (minCount == null) AssertArgCount(c => c <= maxCount, $"{maxCount} or fewer");
		if (maxCount == null) AssertArgCount(c => c >= minCount, $"at least {minCount}");
		else AssertArgCount(c => c >= minCount && c <= maxCount, $"{minCount} to {maxCount}");
	}

    public string GetLineWithoutFirstNArgs(int n)
    {
        string line = Line;
        for (int i = 0; i < n; i++)
        {
            line = line[Args[i].Length..].Trim();
        }
        return line;
    }

    public void Run(Stream stream)
    {
        if (stream.CanSeek) stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);

	    uint lineNum = 0; // 0 == haven't started parsing yet
        string currentSection = "";
		while (reader.ReadLine() is string line)
		{
            lineNum++;
			line = line.Trim();
			if (line == "" || line.StartsWith('#')) continue; // Ignore comments and empty lines

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].ToLower();
            }
            else
            {
                Line = line;
                Args = Regex.Split(line, " +");
                try
                {
                    this[currentSection]?.Invoke(Line, Args);
                }
                catch (Exception e)
                {
                    throw new FileParseException(lineNum, line, e);
                }
            }
        }
    }
}
