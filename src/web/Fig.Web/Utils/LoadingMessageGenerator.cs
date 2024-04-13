namespace Fig.Web.Utils;

public class LoadingMessageGenerator : ILoadingMessageGenerator
{
    private readonly List<string> _loadingMessages;
    private Random _rnd = new();

    public LoadingMessageGenerator()
    {
        _loadingMessages = GenerateLoadingMessages();
    }

    public string GetMessage()
    {
        return _loadingMessages[_rnd.Next(_loadingMessages.Count)];
    }

    private List<string> GenerateLoadingMessages()
    {
        List<string> messages = new List<string>
        {
            "Polishing the silverware",
            "Counting stars",
            "Painting rainbows",
            "Brewing a storm in a teacup",
            "Dreaming in binary",
            "Chasing unicorns",
            "Finding Atlantis",
            "Searching for the meaning of life",
            "Plotting world domination",
            "Exploring parallel universes",
            "Hunting for treasure",
            "Concocting magic spells",
            "Deciphering hieroglyphics",
            "Mixing potions",
            "Building sandcastles in the digital realm",
            "Summoning the spirit of innovation",
            "Solving mysteries",
            "Creating alternate realities",
            "Assembling the Avengers",
            "Balancing the universe",
            "Spinning webs of creativity",
            "Dancing with algorithms",
            "Harnessing the power of imagination",
            "Conquering the final frontier",
            "Juggling ideas",
            "Playing hide and seek with bugs",
            "Whispering secrets to the clouds",
            "Taming wild bytes",
            "Whirling in the chaos of creativity",
            "Sailing on the sea of serendipity",
            "Stirring the cosmic soup",
            "Wrestling with quantum mechanics",
            "Conducting a symphony of progress",
            "Mastering the art of zen coding",
            "Debugging the universe",
            "Embarking on a journey to the center of the code",
            "Chasing rainbows in cyberspace",
            "Unraveling the mysteries of the digital age",
            "Pioneering the future of technology",
            "Tuning into the frequency of innovation",
            "Sculpting with pixels",
            "Exploring the vast expanse of imagination",
            "Navigating the labyrinth of logic",
            "Harvesting the fruits of creativity",
            "Capturing lightning in a bottle",
            "Fishing for ideas in the sea of thought",
            "Weaving dreams into lines of code"
        };

        return messages;
    }
}