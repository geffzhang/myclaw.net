using System.Text.Json;

namespace MyClaw.Core.Entities;

/// <summary>
/// 实体知识图谱存储
/// </summary>
public class EntityStore
{
    private readonly string _entitiesFile;
    private readonly List<Entity> _entities = new();
    private bool _loaded = false;
    private readonly object _lock = new();

    public EntityStore(string workspace)
    {
        _entitiesFile = Path.Combine(workspace, "entities.json");
    }

    /// <summary>
    /// 加载实体数据
    /// </summary>
    public async Task LoadAsync()
    {
        if (_loaded) return;

        lock (_lock)
        {
            if (_loaded) return;

            try
            {
                if (File.Exists(_entitiesFile))
                {
                    var json = File.ReadAllText(_entitiesFile);
                    var data = JsonSerializer.Deserialize<EntityData>(json);
                    if (data?.Entities != null)
                    {
                        _entities.Clear();
                        _entities.AddRange(data.Entities);
                    }
                }
            }
            catch { /* ignore load errors */ }

            _loaded = true;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 保存实体数据
    /// </summary>
    public async Task SaveAsync()
    {
        await LoadAsync();

        lock (_lock)
        {
            var data = new EntityData { Entities = _entities };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_entitiesFile, json);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 添加或更新实体
    /// </summary>
    public async Task<Entity> AddAsync(Entity entity)
    {
        await LoadAsync();

        var now = DateTime.Now.ToString("yyyy-MM-dd");
        var existing = _entities.FirstOrDefault(e =>
            e.Name.Equals(entity.Name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.LastMentioned = now;
            existing.MentionCount++;

            // 合并属性
            foreach (var attr in entity.Attributes)
            {
                existing.Attributes[attr.Key] = attr.Value;
            }

            // 合并关系
            foreach (var rel in entity.Relations)
            {
                if (!existing.Relations.Contains(rel))
                {
                    existing.Relations.Add(rel);
                }
            }

            await SaveAsync();
            return existing;
        }

        // 新实体
        entity.FirstMentioned = now;
        entity.LastMentioned = now;

        lock (_lock)
        {
            _entities.Add(entity);
        }

        await SaveAsync();
        return entity;
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task<bool> RemoveAsync(string name)
    {
        await LoadAsync();

        lock (_lock)
        {
            var idx = _entities.FindIndex(e =>
                e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (idx == -1) return false;

            _entities.RemoveAt(idx);
        }

        await SaveAsync();
        return true;
    }

    /// <summary>
    /// 关联实体
    /// </summary>
    public async Task<bool> LinkAsync(string name, string relation)
    {
        await LoadAsync();

        var entity = _entities.FirstOrDefault(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (entity == null) return false;

        if (!entity.Relations.Contains(relation))
        {
            entity.Relations.Add(relation);
            entity.LastMentioned = DateTime.Now.ToString("yyyy-MM-dd");
            await SaveAsync();
        }

        return true;
    }

    /// <summary>
    /// 查询实体
    /// </summary>
    public async Task<Entity?> QueryAsync(string name)
    {
        await LoadAsync();

        return _entities.FirstOrDefault(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 列出实体
    /// </summary>
    public async Task<List<Entity>> ListAsync(EntityType? filterType = null)
    {
        await LoadAsync();

        if (filterType.HasValue)
        {
            return _entities.Where(e => e.Type == filterType.Value).ToList();
        }

        return _entities.ToList();
    }

    /// <summary>
    /// 获取实体数量
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        await LoadAsync();
        return _entities.Count;
    }

    /// <summary>
    /// 从文本中提取相关实体
    /// </summary>
    public async Task<List<Entity>> SurfaceRelevantAsync(string text)
    {
        await LoadAsync();

        if (string.IsNullOrEmpty(text) || _entities.Count == 0)
            return new List<Entity>();

        var lowerText = text.ToLower();

        return _entities
            .Where(e => lowerText.Contains(e.Name.ToLower()))
            .OrderByDescending(e => e.MentionCount)
            .Take(5)
            .ToList();
    }

    private class EntityData
    {
        public List<Entity> Entities { get; set; } = new();
    }
}
