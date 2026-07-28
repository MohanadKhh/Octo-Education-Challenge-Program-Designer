namespace ProgramDesigner.Application.DTOs;

/// <summary>Request body for POST /programs/:id/simulate.</summary>
public class SimulateProgramRequest
{
    /// <summary>
    /// Selections for Choice groups.
    /// Key: Choice Group Name or Id.
    /// Value: List of selected child Names or Ids.
    /// Example: { "Major": ["AI"], "Electives": ["Computer Vision", "Robotics"] }
    /// </summary>
    public Dictionary<string, List<string>> ChoiceSelections { get; set; } = new();

    /// <summary>
    /// List of completed Step names or Ids.
    /// Example: ["Introduction to Computing", "Mathematics for Computing"]
    /// </summary>
    public List<string> CompletedSteps { get; set; } = new();
}
