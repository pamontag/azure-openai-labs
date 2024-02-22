using Microsoft.Extensions.Hosting;
using OpenAILabs.AIServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAIConsoleTest
{
    public class EmbeddingTestConsole : IHostedService
    {
        readonly IEmbeddingService _embeddingService;
        readonly IHostApplicationLifetime _lifeTime;
        readonly IMemoryStoreService _memoryStoreService;

        public EmbeddingTestConsole(IEmbeddingService embeddingService, IMemoryStoreService memoryStoreService, IHostApplicationLifetime lifeTime)
        {
            ArgumentNullException.ThrowIfNull(lifeTime, nameof(lifeTime));
            ArgumentNullException.ThrowIfNull(embeddingService, nameof(embeddingService));
            ArgumentNullException.ThrowIfNull(memoryStoreService, nameof(memoryStoreService));
            _embeddingService = embeddingService;
            _memoryStoreService = memoryStoreService;
            _lifeTime = lifeTime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => this.ExecuteAsync(cancellationToken), cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Starting Embedding Test"); 
            
            // await StartVolatileMemoryEmbeddingTest();
            await StartBGGAISearchEmbeddingTest();

            Console.WriteLine("Exiting...");

            _lifeTime.StopApplication();
        }

        private async Task StartBGGAISearchEmbeddingTest()
        {
            Console.WriteLine("Searching");
            var queries = new[] {
                "Dammi informazioni generali sul gioco Res Arcana",
                "Descrivimi il gioco Brass Birmingham",
                "Consigliami un gioco ambientato nella civiltà atzeca",
                "Consigliami un gioco ambientato nel far west",
                "Descrivimi il funzionamento del mercato in Brass Birmingham",
                "Descrivimi le azioni possibili in Tzolkin",
                "Dimmi quanti punti salute hanno gli invasori in Spirit Island",
                "Dimmi quante carte si danno ad ogni giocatore in Seasons",
                "Come si calcola il punteggio in KingDomino",
                "Quali sono le tipologie di risorse in Res Arcana",
                "Cosa sono i luoghi di potere in Res Arcana",
                "Consigliami un gioco dalla durata molto breve" };

            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var query in queries)
            {
                var result = await _memoryStoreService.SearchMemoryAsync(query);

                Console.WriteLine($"Query: {query} - Result: {string.Join(",", result)}");

            }
            Console.ResetColor();
        }

        private async Task StartVolatileMemoryEmbeddingTest()
        {
            Console.WriteLine("Starting Memory Store Test");
            var memory = new[] {
                "Spirit Island è un gioco collaborativo da 1 a 4 persone",
                "Spirit Island è un gioco dalla durata di 3 ore",
                "Spirit Island è un gioco di tipo cooperativo",
                "Spirit Island è tra le prime dieci posizioni della classifica BGG",
                "Spirit Island è un gioco molto complesso",
                "Spirit Island è un gioco molto apprezzato",
                "Spirit Island è un gioco di tipo piazzamento tessere",
                "Spirit Island è un gioco di carte",
                "Brass Birmingham è un gioco molto complesso competitivo da 2 a 4 persone",
                "Brass Birmingham è al primo posto della classifica BGG",
                "Brass Birmingham è un gioco di tipo economico",
                "Brass Birmingham è un gioco molto lungo",
                "Brass Birmingham è un gioco molto apprezzato",
                "Brass Birmingham è un gioco di tipo piazzamento tessere",
                "Brass Birmingham è un gioco di carte",
                "Tzolkin è un gioco di tipo german",
                "Tzolk'in è un gioco di tipo worker placement",
                "Tzolk'in è un gioco per 2 o 4 giocatori",
                "Tzolk'in è un gioco dalla durata di 2 ore",
                "Tzolk'in è un gioco molto complesso",
                "Tzolk'in è un gioco tra le prime 50 posizioni della classifica BGG",
                "KingDomino è un gioco per 2 o 4 giocatori",
                "KingDomino è un gioco di tipo tile placement",
                "KingDomino è un gioco molto semplice",
                "KingDomino è un gioco dalla durata di 30 minuti",
                "KingDomino è un gioco dal discreto successo",
                "Great Western Trail è un gioco molto complesso",
                "Great Western Trail è un gioco di tipo worker placement",
                "Great Western Trail è un gioco per 2 o 4 giocatori",
                "Great Western Trail è un gioco molto lungo",
                "Great Western Trail è tra le prime posizioni della classifica BGG",
                "Seasons è un gioco di carte",
                "Seasons è un gioco per 2 o 4 giocatori",
                "Seasons è un gioco dalla durata di un'ora e mezza",
                "Seasons è un gioco dalla complessità media",
                "Seasons è un gioco tra la 100sima e la 200sima posizione della classifica BGG",
                "Res Arcana è un gioco di carte",
                "Res Arcana è un gioco per 2 o 4 giocatori",
                "Res Arcana è un gioco dalla difficoltà media",
                "Res Arcana è un gioco dalla durata di un'ora",
                "Res Arcana è un gioco molto apprezzato",
            };

            Console.WriteLine("Storing");
            foreach (var item in memory)
            {
                var id = await _memoryStoreService.StoreMemoryAsync(item, null);
                Console.WriteLine($"Stored: {item} - Id: {id}");
            }

            Console.WriteLine("Searching");
            var queries = new[] {
                "Dammi informazioni sul gioco Res Arcana",
                "Consigliami un gioco complesso",
                "Consigliami un gioco di tipo worker placement",
                "Consigliami un gioco per 3 giocatori",
                "Consigliami un gioco acclamato dalla critica",
                "Consigliami un gioco nelle prime posizioni della classifica BGG",
                "Consigliami un gioco che abbia carte al suo interno",
                "Consigliami un gioco dalla durata non superiore a 1 ora",
                "Elencami alcuni giochi dalla bassa difficoltà" };

            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var query in queries)
            {
                var result = await _memoryStoreService.SearchMemoryAsync(query);

                Console.WriteLine($"Query: {query} - Result: {string.Join(",", result)}");

            }
            Console.ResetColor();
        }
    }
}
