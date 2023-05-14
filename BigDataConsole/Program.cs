using Bogus;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net.Http.Json;
using DAL.Model;
using Person = DAL.Model.Person;
using DAL;
using Microsoft.EntityFrameworkCore;
using System;

CancellationTokenSource APIcts = new CancellationTokenSource();
CancellationToken APIcancellationToken = APIcts.Token;

CancellationTokenSource LocalCTS = new CancellationTokenSource();
CancellationToken LocalcancellationToken = LocalCTS.Token;


HttpClient client = new HttpClient();

Console.WriteLine(@"1. Start API
2. Stop API
3. Start Collection
4. Stop Collection
5. Get Newest
X. Exit
");
while (true)
{
    switch (Console.ReadKey().Key)
    {
        case ConsoleKey.D1:
            new Task(() => CreateData(APIcancellationToken)).Start();
            Console.WriteLine("\nstarted api gen");
            break;

        case ConsoleKey.D2:
            APIcts.Cancel();
            Console.WriteLine("\nstopped api gen");
            break;

        case ConsoleKey.D3:
            new Task(() => StartCollecting(LocalcancellationToken)).Start();
            Console.WriteLine("\nstarted collection");
            break;

        case ConsoleKey.D4:
            LocalCTS.Cancel();
            Console.WriteLine("\nstopped collection");
            break;

        case ConsoleKey.D5:
            await GetNewestPerson();
            break;

        case ConsoleKey.X:
            Environment.Exit(0);
            break;

        default:
            break;
    }
}



async Task GetNewestPerson()
{
    using (var _context = new BigDataContext())
    {
        Person person = await _context.People.OrderBy(x => x.Id).LastAsync();
        await Console.Out.WriteLineAsync(person.ToString());
    }
}
async Task CreateData(CancellationToken token)
{

    while (true)
    {
        Faker<Person> Faker = new Faker<Person>();
        Faker.RuleFor(x => x.FirstName, x => x.Person.FirstName)
        .RuleFor(x => x.LastName, x => x.Person.LastName)
        .RuleFor(x => x.DateOfBirth, x => DateTime.Now);
        Person person = Faker.Generate();

        await client.PostAsJsonAsync<Person>($"https://localhost:7132/api/Person/CreateNewPerson/", person);
        await Console.Out.WriteLineAsync("\ncreated data");
        if (token.IsCancellationRequested)
        {
            break;
        }
        Task.Delay(5000).Wait();
    }
}

async Task StartCollecting(CancellationToken token)
{
    while (true)
    {
        Person person = await client.GetFromJsonAsync<Person>("https://localhost:7132/api/Person/GetNewestPerson");
        using (var _context = new BigDataContext())
        {
            await _context.People.AddAsync(person);
            await _context.SaveChangesAsync();
        }
        await Console.Out.WriteLineAsync("\ninserted data");
        if (token.IsCancellationRequested)
        {
            break;
        }
        Task.Delay(5000).Wait();
    }
}