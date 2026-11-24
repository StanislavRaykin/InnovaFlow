using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Anthropic.Helpers;

namespace Analysis.Worker.AI;

/// <summary>
/// Builds dummy instances of structured-output types for <see cref="FakeAiClient"/>. It reads each property's
/// [SchemaProperty] so the result follows the same rules the model gets:
/// Minimum/Maximum for numbers, "2-6 ..." / "up to 3" / "exactly 3" for list sizes, "1-3 sentences" for text length,
/// numbered "1) ... 2) ..." lists for paragraphs, "e.g. '...'" examples, "exactly one of 'a', 'b'" choices and "Start with 'X'".
/// New result types work without changes here.
/// </summary>
internal static class FakeData
{
    private const int MaxDepth = 5;
    private const string Marker = "[ТЕСТОВИ ДАННИ] ";

    public static T Create<T>(Random rng) where T : class, new() => (T)CreateObject(typeof(T), rng, index: null, depth: 0);

    /// <summary>Markdown research notes citing <paramref name="sources"/> by number, like the real research step.</summary>
    public static string ResearchNotes(string title, IReadOnlyList<AiSource> sources, Random rng)
    {
        string Cite() => sources.Count == 0 ? "" : $" [{sources[rng.Next(sources.Count)].Number}]";

        var sb = new StringBuilder();
        sb.AppendLine($"{Marker}Проучване за „{title}“, генерирано без реално търсене в интернет.");
        sb.AppendLine();
        sb.AppendLine("## Размер и тенденции на пазара");
        foreach (var s in PickMany(rng, Sentences, 3)) sb.AppendLine($"- {s}{Cite()}");
        sb.AppendLine();
        sb.AppendLine("## Конкуренти");
        for (var i = 1; i <= 3; i++)
            sb.AppendLine($"- Тестов конкурент {i}: {Pick(rng, Phrases["Strength"]).ToLowerInvariant()}, но {Pick(rng, Phrases["Weakness"]).ToLowerInvariant()}.{Cite()}");
        sb.AppendLine();
        sb.AppendLine("## Ценообразуване");
        foreach (var s in PickMany(rng, Sentences, 2)) sb.AppendLine($"- {s}{Cite()}");
        return sb.ToString();
    }

    // ── objects and values ──────────────────────────────────────────

    private static object CreateObject(Type type, Random rng, int? index, int depth)
    {
        var obj = Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Cannot create {type.Name}");
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanWrite || p.GetIndexParameters().Length > 0) continue;
            if (p.GetCustomAttribute<JsonIgnoreAttribute>() is { Condition: JsonIgnoreCondition.Always }) continue;

            p.SetValue(obj, CreateValue(p, p.PropertyType, rng, index, depth));
        }
        return obj;
    }

    private static object? CreateValue(PropertyInfo p, Type type, Random rng, int? index, int depth)
    {
        var attr = p.GetCustomAttribute<SchemaPropertyAttribute>();
        var desc = attr?.Description ?? "";
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
        {
            // optional strings (e.g. WebsiteUrl) are sometimes left out, like the model does
            if (IsNullable(p) && rng.Next(4) == 0) return null;
            return Text(p.Name, desc, rng, index);
        }
        if (type == typeof(bool)) return rng.Next(2) == 0;
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(rng.Next(values.Length));
        }
        if (IsNumber(type)) return Convert.ChangeType(Number(p.Name, attr, rng), type);
        if (type == typeof(Guid)) return new Guid(Enumerable.Range(0, 16).Select(_ => (byte)rng.Next(256)).ToArray());
        if (type == typeof(DateTime)) return DateTime.UtcNow;
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.UtcNow;
        if (ElementType(type) is { } element) return CreateList(p, type, element, desc, rng, depth);
        if (type.IsClass && depth < MaxDepth) return CreateObject(type, rng, index: null, depth + 1);

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static object? CreateList(PropertyInfo p, Type type, Type element, string desc, Random rng, int depth)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
        var count = Count(desc, rng);
        if (element == typeof(string))
        {
            foreach (var phrase in PickMany(rng, PhrasesFor(p.Name), count)) list.Add(phrase);   // no repeats
        }
        else if (element.IsClass && depth < MaxDepth)
        {
            for (var i = 1; i <= count; i++) list.Add(CreateObject(element, rng, index: i, depth + 1));
        }

        if (type.IsArray)
        {
            var array = Array.CreateInstance(element, list.Count);
            list.CopyTo(array, 0);
            return array;
        }
        return type.IsAssignableFrom(list.GetType()) ? list : null;
    }

    // ── numbers ─────────────────────────────────────────────────────

    private static double Number(string name, SchemaPropertyAttribute? attr, Random rng)
    {
        var min = attr is { HasMinimum: true } ? attr.Minimum : 1;
        var max = attr is { HasMaximum: true } ? attr.Maximum : name.Contains("Score", StringComparison.OrdinalIgnoreCase) ? 100 : 10;
        // stay away from the extremes so it looks like a real result
        var lo = min + (max - min) * 0.3;
        var hi = min + (max - min) * 0.9;
        return Math.Round(lo + rng.NextDouble() * (hi - lo));
    }

    private static bool IsNumber(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
        t == typeof(double) || t == typeof(float) || t == typeof(decimal);

    // ── text ────────────────────────────────────────────────────────

    private static string Text(string name, string desc, Random rng, int? index)
    {
        if (Has(name, "Url", "Website", "Link"))
            return $"https://example.com/{name.ToLowerInvariant()}{(index is { } i ? $"-{i}" : "")}";

        // "Must contain, in this order: 1) ... 2) ..." -> one paragraph per item
        var paragraphs = NumberedItemRegex.Matches(desc).Count;
        if (paragraphs >= 2)
            return Marker + string.Join("\n\n", Enumerable.Range(0, paragraphs).Select(_ => string.Join(" ", PickMany(rng, Sentences, 2))));

        // "2-5 short points separated by new lines"
        if (desc.Contains("new line", StringComparison.OrdinalIgnoreCase))
            return string.Join("\n", PickMany(rng, PhrasesFor(name), Count(desc, rng)).Select(s => "- " + s));

        // "exactly one of 'ниска', 'средна', 'висока', followed by a short reason"
        if (Quoted(OneOfRegex.Match(desc)) is { Count: > 0 } choices)
            return $"{Pick(rng, choices)} — тестова обосновка";

        // "Start with 'Пряк конкурент.' or 'Косвен конкурент.', then ..."
        if (Quoted(StartWithRegex.Match(desc)) is { Count: > 0 } starts)
            return $"{Pick(rng, starts)} {string.Join(" ", PickMany(rng, Sentences, rng.Next(1, 3)))}";

        var sentences = SentenceRangeRegex.Match(desc);
        if (sentences.Success)
            return string.Join(" ", PickMany(rng, Sentences, rng.Next(int.Parse(sentences.Groups[1].Value), int.Parse(sentences.Groups[2].Value) + 1)));

        // short fields with an example: "e.g. 'Мария — заета студентка'", "e.g. 'Абонамент', 'Комисионна'"
        if (!desc.Contains("sentence", StringComparison.OrdinalIgnoreCase) && Quoted(ExampleRegex.Match(desc)) is { Count: > 0 } examples)
        {
            var example = Pick(rng, examples);
            // list items get distinct names (Innovation dedupes competitors by name)
            return index is > 1 && Has(name, "Name", "Title") ? $"{example} (вариант {index})" : example;
        }

        if (desc.Contains("sentence", StringComparison.OrdinalIgnoreCase) ||
            Has(name, "Summary", "Description", "Explanation", "Text", "Notes", "Reason"))
            return string.Join(" ", PickMany(rng, Sentences, 2));

        if (Has(name, "Name", "Title"))
            return $"Тестово име {index ?? 1}";

        return Pick(rng, PhrasesFor(name));
    }

    /// <summary>"exactly 3" -> 3, "up to 3" -> 2-3, "2-6" -> 2..6, otherwise 2-3</summary>
    private static int Count(string desc, Random rng)
    {
        if (ExactlyRegex.Match(desc) is { Success: true } exactly) return int.Parse(exactly.Groups[1].Value);
        if (UpToRegex.Match(desc) is { Success: true } upTo)
        {
            var max = int.Parse(upTo.Groups[1].Value);
            return rng.Next(Math.Max(1, max - 1), max + 1);
        }
        if (RangeRegex.Match(desc) is { Success: true } range)
            return rng.Next(int.Parse(range.Groups[1].Value), int.Parse(range.Groups[2].Value) + 1);
        return rng.Next(2, 4);
    }

    // ── helpers ─────────────────────────────────────────────────────

    private static bool Has(string name, params string[] parts) =>
        parts.Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase));

    private static bool IsNullable(PropertyInfo p) =>
        new NullabilityInfoContext().Create(p).WriteState == NullabilityState.Nullable;   // not thread-safe, so not shared

    private static Type? ElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType();
        return type.IsGenericType && type.GetGenericArguments().Length == 1 && typeof(IEnumerable).IsAssignableFrom(type)
            ? type.GetGenericArguments()[0]
            : null;
    }

    private static List<string> Quoted(Match m) =>
        m.Success ? QuotedRegex.Matches(m.Groups[1].Value).Select(q => q.Groups[1].Value).ToList() : [];

    private static string Pick(Random rng, IReadOnlyList<string> items) => items[rng.Next(items.Count)];

    private static IEnumerable<string> PickMany(Random rng, IReadOnlyList<string> items, int count) =>
        items.OrderBy(_ => rng.Next()).Take(count);

    private static string[] PhrasesFor(string name) =>
        Phrases.FirstOrDefault(kv => kv.Key.Length > 0 && name.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)).Value ?? Phrases[""];

    // A list of quoted values: 'a', 'b' or 'c'
    private const string QuotedList = @"((?:'[^']*'[,\s]*(?:or\s+)?)+)";

    private static readonly Regex NumberedItemRegex = new(@"(?<![\w-])\d\)");
    private static readonly Regex SentenceRangeRegex = new(@"(\d+)\s*-\s*(\d+)\s+(?:short\s+)?sentences", RegexOptions.IgnoreCase);
    private static readonly Regex OneOfRegex = new(@"exactly one of\s+" + QuotedList, RegexOptions.IgnoreCase);
    private static readonly Regex StartWithRegex = new(@"start with\s+" + QuotedList, RegexOptions.IgnoreCase);
    private static readonly Regex ExampleRegex = new(@"e\.g\.\s*" + QuotedList, RegexOptions.IgnoreCase);
    private static readonly Regex QuotedRegex = new(@"'([^']*)'");
    private static readonly Regex ExactlyRegex = new(@"exactly\s+(\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex UpToRegex = new(@"up to\s+(\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex RangeRegex = new(@"(\d+)\s*-\s*(\d+)");

    // ── word pools ──────────────────────────────────────────────────

    private static readonly string[] Sentences =
    [
        "Оценката е базирана на тестови данни и не отразява реален анализ.",
        "Идеята адресира реален проблем, но пазарът вече предлага частични алтернативи.",
        "Ключовото предимство е по-доброто потребителско изживяване спрямо съществуващите решения.",
        "Основната несигурност е готовността на потребителите да плащат за услугата.",
        "Препоръчително е да се проведат интервюта с поне десет потенциални потребители.",
        "Конкуренцията е умерена, като повечето играчи са фокусирани върху по-големи пазари.",
        "Технологичната реализация е постижима с наличните днес инструменти.",
        "Растежът зависи силно от ефективността на първите маркетингови канали.",
        "Нивото на увереност в тази оценка е средно поради ограничените входни данни.",
        "Малък пилотен тест би потвърдил или отхвърлил основните предположения.",
        "Бизнес моделът може да комбинира абонамент и допълнителни платени функции.",
        "Регулаторните изисквания не изглеждат значима пречка на този етап.",
        "Местният пазар е малък, но служи добре за първоначална проверка на идеята.",
        "Диференциацията трябва да се търси в нишов сегмент, а не в цената."
    ];

    // keyed by a part of the property name; "" is the fallback
    private static readonly Dictionary<string, string[]> Phrases = new()
    {
        ["Strength"] = ["Разпознаваема марка", "Голяма потребителска база", "Добре развито мобилно приложение", "Широка мрежа от партньори", "Конкурентни цени"],
        ["Weakness"] = ["Високи комисионни", "Слаба локализация за българския пазар", "Бавна поддръжка на клиенти", "Ограничена персонализация", "Сложна регистрация"],
        ["Goal"] = ["Да спестява време в ежедневието", "Да харчи по-малко пари", "Да получава персонализирани препоръки", "Да има повече контрол над процеса"],
        ["Pain"] = ["Губи време в търсене на подходящо решение", "Липса на прозрачност в цените", "Неудобни съществуващи инструменти", "Твърде много разпокъсани приложения"],
        ["Behavio"] = ["Използва смартфона си по няколко часа на ден", "Сравнява цени преди покупка", "Доверява се на препоръки от приятели", "Следи социалните мрежи вечер"],
        ["Industry"] = ["Информационни технологии", "Електронна търговия", "Образование", "Финансови услуги"],
        ["Occupation"] = ["Студент", "Софтуерен инженер", "Маркетинг специалист", "Собственик на малък бизнес"],
        ["Trend"] = ["Ръст на мобилните покупки", "Засилен интерес към персонализация", "Навлизане на AI асистенти", "Търсене на устойчиви решения", "Повече абонаментни модели"],
        [""] = ["Бърз старт с минимален бюджет", "Фокус върху една ниша", "Партньорства с местни бизнеси", "Пилотен проект с ранни потребители", "Ясно ценово предложение"]
    };
}
