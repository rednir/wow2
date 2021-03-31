using System.Collections.Generic;
using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace wow2.Modules.Games
{
    public abstract class GameConfigBase
    {
        public ICommandContext InitalContext { get; set; }
    }

    public class GamesModuleConfig
    {
        [JsonIgnore]
        public CountingGameConfig Counting { get; set; } = new CountingGameConfig();

        [JsonIgnore]
        public VerbalMemoryGameConfig VerbalMemory { get; set; } = new VerbalMemoryGameConfig();
    }

    public class CountingGameConfig : GameConfigBase
    {
        public float Increment { get; set; }
        public List<SocketMessage> ListOfMessages { get; set; } = new List<SocketMessage>();

        /// <summary>Represents the next correct number when counting, or null if counting has ended.</summary>
        public float? NextNumber { get; set; }
    }

    public class VerbalMemoryGameConfig : GameConfigBase
    {
        public List<string> UnseenWords { get; set; } = new List<string>() { "aberrant", "aconite", "agglutinates", "alegar", "ammonal", "amphibrachs", "anteing", "arachnophobia", "arak", "armings", "armrest", "assortment", "atony", "attaching", "attic", "baffling", "balsas", "baserunner", "bdellium", "beds", "belligerent", "beltway", "bichromates", "bigmouth", "bird", "blacking", "blague", "blatantly", "bluegills", "bolster", "bonehead", "bookmarked", "bootlegs", "borrower", "bourgeoisify", "boycotts", "brackets", "brawler", "breathers", "broadmindedness", "brokered", "broncobuster", "brutishness", "burthens", "byword", "caprioles", "caseate", "cauda", "causer", "ceases", "ceratoduses", "certifiably", "chondrule", "circumfluent", "clamped", "clamping", "clarabella", "clarinets", "classy", "claudications", "clefts", "clouted", "cloyingness", "codpiece", "cognate", "cogs", "colonel", "comedo", "commemorators", "compel", "compensations", "complementarily", "conduced", "congratulations", "consistence", "contestations", "conversing", "cosmologically", "cowsheds", "crankcase", "craved", "cupids", "customs", "danseuses", "dapple", "deduction", "deference", "deglutinate", "deputations", "design", "dime", "discovert", "dished", "disobliged", "disparatenesses", "displumes", "distichous", "divulgences", "dollarfish", "doubts", "dowdy", "drably", "dratted", "duodenal", "dwindling", "dynamometry", "effacer", "elapsing", "electrokinetics", "elusions", "endostosis", "engulfed", "enshroud", "epidermic", "era", "errorless", "escargot", "esterase", "exaggeratory", "extemporaneity", "farina", "fielded", "figurines", "flauntier", "fleshed", "flicks", "flintlocks", "form", "formed", "forum", "frailly", "freshen", "frontbenches", "froths", "gainsaying", "gallinule", "genips", "gentlefolk", "glabrous", "glossarial", "hackable", "hautbois", "helical", "herbivorous", "hobbies", "holdups", "honeys", "hopple", "humbler", "humblest", "humping", "hyperventilation", "hypocrites", "hysterogenic", "ignite", "impersonate", "impound", "impuissance", "inasmuch", "incisive", "indissolubility", "industrial", "inelegance", "inion", "interacting", "intermission", "interspersing", "intervallic", "isotope", "isthmuses", "italic", "jato", "jumpier", "kalmias", "kebabs", "keelboat", "khaddar", "kobs", "la", "laborious", "lamination", "lampshade", "leaguing", "learnedness", "lexicology", "liens", "locomotion", "logan", "lookup", "loudish", "lunitidal", "madrases", "madro�as", "malamute", "mashup", "meant", "medicos", "meditational", "methenamine", "microlithic", "migrated", "minke", "mirthlessly", "movablenesses", "muchness", "multitudinously", "nectarous", "needed", "niblick", "noncandidate", "nonelective", "nosebags", "nutritional", "objected", "obsessed", "obsoleteness", "odiousness", "omitted", "opium", "optimistic", "ostomy", "outbound", "outsells", "overcurious", "overhauls", "ovulates", "oxidants", "packinghouses", "painstaking", "papains", "patinated", "pendular", "peptides", "phosphates", "photochronograph", "picot", "pikeperch", "pillowslip", "pinchcock", "placarding", "planetariums", "plotted", "popish", "pounded", "prepay", "prescriber", "presto", "primetime", "prison", "profiling", "progressivity", "prolabor", "pronounced", "pudgiest", "puppets", "pushpin", "pyrology", "pyrrhuloxia", "quixotry", "randan", "rearguards", "reaved", "rebus", "recces", "recondition", "redecorated", "reexamination", "registrations", "reign", "rejoicer", "remonstrantly", "renowned", "repentant", "repulsed", "resectional", "resounding", "restaurant", "returner", "reverb", "ritzier", "robustness", "ropiest", "rouses", "rubricated", "rudderposts", "saltily", "sandwich", "satinwoods", "schizoids", "screeching", "scrunchy", "seasonably", "semipermeable", "sensationally", "sett", "silage", "slavers", "songstress", "sorbing", "soutane", "spittoons", "splendidest", "squeegee", "stander", "sternutation", "stilliest", "stressing", "structures", "stubbier", "studs", "stumping", "stuntmen", "stuttered", "subdirector", "subjectify", "succinctly", "supernovas", "surfeiting", "swaps", "sweller", "symbolic", "tangents", "tawninesses", "telegraphic", "tempest", "tenaille", "tepidity", "theatrician", "thirsty", "throat", "thwacks", "tick", "tiresome", "toggery", "toroid", "transferable", "transsexualism", "trappable", "travailing", "trematode", "tributes", "trigram", "triskaidekaphobia", "tsarevitch", "tusche", "twit", "typhogenic", "understandingly", "unfettered", "unfitness", "unhappy", "unifilar", "unrequited", "vainglory", "valiancies", "veining", "verification", "vesical", "villeinage", "vinery", "wagtails", "washed", "welshes", "whaleboats", "whirled", "willy", "wolframite", "woodnote", "wooliness", "xebec" };
        public List<string> SeenWords { get; set; } = new List<string>();
        public int Turns { get; set; } = 0;

        /// <summary>Represents the message with the current word, or null if the game has ended.</summary>
        public IUserMessage CurrentWordMessage { get; set; }
    }
}