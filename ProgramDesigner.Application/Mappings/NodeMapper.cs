using ProgramDesigner.Application.DTOs;
using ProgramDesigner.Domain.Entities;
using ProgramDesigner.Domain.Enums;

namespace ProgramDesigner.Application.Mappings;

/// <summary>
/// Static mapper between NodeDto ↔ Node (recursive tree mapping).
/// Option A Architecture: PrerequisiteId inputs with PrerequisiteId + PrerequisiteName outputs.
/// Non-throwing validation via TryToDomain.
/// </summary>
public static class NodeMapper
{
    /// <summary>
    /// Safely map a NodeDto tree (from API request) to a domain Node tree.
    /// Returns false and populates <paramref name="errors"/> if validation fails.
    /// </summary>
    public static bool TryToDomain(
        NodeDto dto,
        Guid programId,
        out Node? root,
        out Dictionary<string, string[]>? errors,
        int order = 0)
    {
        var errorList = new List<string>();
        var allNodeIds = new HashSet<Guid>();

        // Pass 1: Build tree and collect node IDs + syntax validation
        root = BuildNode(dto, programId, order, allNodeIds, errorList);

        // Pass 2: Validate that all PrerequisiteId references point to an existing node in the tree
        if (root is not null)
        {
            ValidatePrerequisiteIds(root, allNodeIds, errorList);
        }

        if (errorList.Count > 0)
        {
            errors = new Dictionary<string, string[]>
            {
                { "NodeMapping", errorList.ToArray() }
            };
            root = null;
            return false;
        }

        errors = null;
        return true;
    }

    /// <summary>
    /// Fallback helper for ToDomain.
    /// </summary>
    public static Node ToDomain(NodeDto dto, Guid programId, int order = 0)
    {
        if (!TryToDomain(dto, programId, out var root, out var errors, order))
        {
            var firstError = errors?.Values.FirstOrDefault()?.FirstOrDefault() ?? "Invalid node mapping.";
            throw new ArgumentException(firstError);
        }
        return root!;
    }

    private static Node? BuildNode(
        NodeDto dto, Guid programId, int order,
        HashSet<Guid> allNodeIds,
        List<string> errorList)
    {
        if (dto is null)
        {
            errorList.Add("Node payload cannot be null.");
            return null;
        }

        NodeType nodeType = NodeType.Step;
        if (string.IsNullOrWhiteSpace(dto.Type))
        {
            errorList.Add($"Node '{dto.Name}' must specify a type ('step' or 'group').");
        }
        else
        {
            switch (dto.Type.ToLowerInvariant())
            {
                case "step":
                    nodeType = NodeType.Step;
                    break;
                case "group":
                    nodeType = NodeType.Group;
                    break;
                default:
                    errorList.Add($"Unknown node type '{dto.Type}' on node '{dto.Name}'. Expected 'step' or 'group'.");
                    break;
            }
        }

        GroupRule? rule = null;
        if (nodeType == NodeType.Group)
        {
            if (string.IsNullOrWhiteSpace(dto.Rule))
            {
                errorList.Add($"Group node '{dto.Name}' must specify a rule ('inOrder' or 'choice').");
            }
            else
            {
                switch (dto.Rule.ToLowerInvariant())
                {
                    case "inorder":
                    case "in_order":
                        rule = GroupRule.InOrder;
                        break;
                    case "choice":
                        rule = GroupRule.Choice;
                        break;
                    default:
                        errorList.Add($"Unknown group rule '{dto.Rule}' on node '{dto.Name}'. Expected 'inOrder' or 'choice'.");
                        break;
                }
            }
        }
        else if (nodeType == NodeType.Step && !string.IsNullOrWhiteSpace(dto.Rule))
        {
            errorList.Add($"Step node '{dto.Name}' cannot have a group rule ('{dto.Rule}'). Only Group nodes can specify rules.");
        }

        var nodeId = dto.Id ?? Guid.NewGuid();

        if (!allNodeIds.Add(nodeId))
        {
            errorList.Add($"Duplicate node ID '{nodeId}' found on node '{dto.Name}'. Every node must have a unique ID.");
        }

        var node = new Node
        {
            Id = nodeId,
            Name = dto.Name,
            Type = nodeType,
            StepType = nodeType == NodeType.Step ? dto.StepType : null,
            Rule = rule,
            ChoiceCount = rule == GroupRule.Choice ? dto.ChoiceCount : null,
            PrerequisiteId = dto.PrerequisiteId,
            Order = order,
            ProgramId = programId
        };

        if (dto.Children is not null && dto.Children.Count > 0)
        {
            if (nodeType == NodeType.Step)
            {
                errorList.Add($"Step node '{dto.Name}' cannot have children. Only Group nodes can contain child nodes.");
            }
            else
            {
                for (int i = 0; i < dto.Children.Count; i++)
                {
                    var child = BuildNode(dto.Children[i], programId, i, allNodeIds, errorList);
                    if (child is not null)
                    {
                        child.ParentNodeId = node.Id;
                        node.Children.Add(child);
                    }
                }
            }
        }

        return node;
    }

    private static void ValidatePrerequisiteIds(
        Node node,
        HashSet<Guid> allNodeIds,
        List<string> errorList)
    {
        if (node.PrerequisiteId.HasValue)
        {
            if (!allNodeIds.Contains(node.PrerequisiteId.Value))
            {
                errorList.Add($"Node '{node.Name}' has prerequisiteId '{node.PrerequisiteId.Value}', but no node with that ID exists in the program tree.");
            }
        }

        foreach (var child in node.Children)
            ValidatePrerequisiteIds(child, allNodeIds, errorList);
    }

    public static NodeDto ToDto(Node node, Dictionary<Guid, string>? idToNameIndex = null)
    {
        string? prereqName = null;
        if (node.PrerequisiteId.HasValue && idToNameIndex is not null)
        {
            idToNameIndex.TryGetValue(node.PrerequisiteId.Value, out prereqName);
        }

        List<NodeDto>? children = null;
        if (node.Children is not null && node.Children.Count > 0)
        {
            children = node.Children
                .OrderBy(c => c.Order)
                .Select(c => ToDto(c, idToNameIndex))
                .ToList();
        }

        return new NodeDto
        {
            Id = node.Id,
            Name = node.Name,
            Type = node.Type == NodeType.Step ? "step" : "group",
            StepType = node.StepType,
            Rule = node.Rule switch
            {
                GroupRule.InOrder => "inOrder",
                GroupRule.Choice => "choice",
                _ => null
            },
            ChoiceCount = node.ChoiceCount,
            PrerequisiteId = node.PrerequisiteId,
            PrerequisiteName = prereqName,
            Children = children
        };
    }

    public static ProgramResponse ToResponse(LearningProgram program)
    {
        var idToNameIndex = new Dictionary<Guid, string>();
        if (program.RootNode is not null)
            BuildIdToNameIndex(program.RootNode, idToNameIndex);

        return new ProgramResponse
        {
            Id = program.Id,
            Name = program.Name,
            RootNode = program.RootNode is not null ? ToDto(program.RootNode, idToNameIndex) : null!,
            CreatedAt = program.CreatedAt
        };
    }

    private static void BuildIdToNameIndex(Node node, Dictionary<Guid, string> index)
    {
        if (!string.IsNullOrWhiteSpace(node.Name))
            index[node.Id] = node.Name;

        foreach (var child in node.Children)
            BuildIdToNameIndex(child, index);
    }
}
