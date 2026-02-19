namespace MyClaw.Core.Workspace;

/// <summary>
/// 技术栈检测器 - 通过文件特征检测项目使用的技术
/// </summary>
public static class TechStackDetector
{
    // 技术栈文件映射表
    private static readonly Dictionary<string, string[]> TechFileMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // JavaScript/TypeScript 生态
        ["Node.js"] = new[] { "package.json", "package-lock.json", "yarn.lock", "pnpm-lock.yaml" },
        ["TypeScript"] = new[] { "tsconfig.json" },
        ["React"] = new[] { "vite.config.ts", "vite.config.js", "next.config.js", "next.config.ts", "react.config.js" },
        ["Vue"] = new[] { "vue.config.js", "vite.config.ts", "nuxt.config.ts" },
        ["Angular"] = new[] { "angular.json" },
        ["Svelte"] = new[] { "svelte.config.js" },
        
        // Python 生态
        ["Python"] = new[] { "requirements.txt", "setup.py", "pyproject.toml", "Pipfile", "poetry.lock" },
        ["Django"] = new[] { "manage.py", "settings.py", "wsgi.py" },
        ["Flask"] = new[] { "app.py", "wsgi.py" },
        ["FastAPI"] = new[] { "main.py", "app/main.py" },
        
        // .NET 生态
        [".NET"] = new[] { "*.csproj", "*.sln", "*.slnx" },
        ["C#"] = new[] { "*.csproj", "Program.cs" },
        ["F#"] = new[] { "*.fsproj" },
        ["VB.NET"] = new[] { "*.vbproj" },
        
        // Java 生态
        ["Java"] = new[] { "pom.xml", "build.gradle", "build.gradle.kts", "gradlew" },
        ["Spring Boot"] = new[] { "application.properties", "application.yml", "spring-boot-starter" },
        ["Maven"] = new[] { "pom.xml" },
        ["Gradle"] = new[] { "build.gradle", "build.gradle.kts", "gradlew" },
        
        // Go 生态
        ["Go"] = new[] { "go.mod", "go.sum" },
        
        // Rust 生态
        ["Rust"] = new[] { "Cargo.toml", "Cargo.lock" },
        
        // Ruby 生态
        ["Ruby"] = new[] { "Gemfile", "Gemfile.lock", "*.gemspec" },
        ["Ruby on Rails"] = new[] { "config/routes.rb", "config/application.rb" },
        
        // PHP 生态
        ["PHP"] = new[] { "composer.json", "composer.lock" },
        ["Laravel"] = new[] { "artisan", "phpunit.xml" },
        ["Symfony"] = new[] { "symfony.lock", "config/bundles.php" },
        
        // 容器化
        ["Docker"] = new[] { "Dockerfile", "docker-compose.yml", "docker-compose.yaml", ".dockerignore" },
        ["Kubernetes"] = new[] { "k8s", "kubernetes", "deployment.yaml", "service.yaml", "*.k8s.yaml" },
        
        // 基础设施
        ["Terraform"] = new[] { "*.tf", "terraform.tfstate" },
        ["Ansible"] = new[] { "ansible.cfg", "playbook.yml", "inventory" },
        ["Pulumi"] = new[] { "Pulumi.yaml", "Pulumi.*.yaml" },
        
        // 数据库
        ["PostgreSQL"] = new[] { "docker-compose.yml", "*.sql" },
        ["MySQL"] = new[] { "docker-compose.yml", "*.sql" },
        ["MongoDB"] = new[] { "docker-compose.yml", "*.mongodb" },
        ["Redis"] = new[] { "redis.conf", "docker-compose.yml" },
        
        // 配置管理
        ["Nix"] = new[] { "flake.nix", "shell.nix", "default.nix" },
        ["Homebrew"] = new[] { "Brewfile" },
        
        // 前端构建工具
        ["Vite"] = new[] { "vite.config.ts", "vite.config.js" },
        ["Webpack"] = new[] { "webpack.config.js", "webpack.config.ts" },
        ["Rollup"] = new[] { "rollup.config.js" },
        ["Parcel"] = new[] { ".parcelrc" },
        ["esbuild"] = new[] { "esbuild.config.js" },
        
        // 测试框架
        ["Jest"] = new[] { "jest.config.js", "jest.config.ts" },
        ["Vitest"] = new[] { "vitest.config.ts", "vitest.config.js" },
        ["pytest"] = new[] { "pytest.ini", "conftest.py" },
        ["xUnit"] = new[] { "*.Tests.csproj", "*Test*.csproj" },
        
        // CI/CD
        ["GitHub Actions"] = new[] { ".github/workflows" },
        ["GitLab CI"] = new[] { ".gitlab-ci.yml" },
        ["Azure DevOps"] = new[] { "azure-pipelines.yml" },
        ["Jenkins"] = new[] { "Jenkinsfile" },
        ["CircleCI"] = new[] { ".circleci", ".circleci/config.yml" },
        ["Travis CI"] = new[] { ".travis.yml" },
        
        // 其他
        ["CMake"] = new[] { "CMakeLists.txt" },
        ["Make"] = new[] { "Makefile", "makefile", "GNUmakefile" },
        ["Ninja"] = new[] { "build.ninja" },
        ["Bazel"] = new[] { "WORKSPACE", "BUILD", "BUILD.bazel", ".bazelrc" },
        ["Buck"] = new[] { ".buckconfig" },
        ["Pants"] = new[] { "pants.toml" },
    };

    /// <summary>
    /// 检测指定目录的技术栈
    /// </summary>
    public static List<string> Detect(string directory)
    {
        var detected = new List<string>();
        
        if (!Directory.Exists(directory))
        {
            return detected;
        }

        try
        {
            // 获取目录中的所有文件（不包括子目录）
            var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Select(f => Path.GetFileName(f))
                .ToList();

            // 检查子目录（用于检测框架）
            var directories = Directory.GetDirectories(directory)
                .Select(d => Path.GetFileName(d))
                .ToList();

            foreach (var (tech, patterns) in TechFileMap)
            {
                if (IsTechDetected(files, directories, patterns))
                {
                    detected.Add(tech);
                }
            }

            // 去重并排序
            return detected.Distinct().OrderBy(t => t).ToList();
        }
        catch
        {
            return detected;
        }
    }

    private static bool IsTechDetected(List<string> files, List<string> directories, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            // 检查是否是目录模式（无点号、无通配符，且包含路径分隔符或是已知目录名）
            if (!pattern.Contains('.') && !pattern.Contains('*') && !pattern.Contains('/'))
            {
                // 先检查文件（如 Dockerfile、Makefile）
                if (files.Contains(pattern, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
                // 再检查目录
                if (directories.Contains(pattern, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
                continue;
            }

            // 通配符匹配
            if (pattern.Contains('*'))
            {
                var regex = new System.Text.RegularExpressions.Regex(
                    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (files.Any(f => regex.IsMatch(f)))
                {
                    return true;
                }
            }
            // 精确匹配
            else if (files.Contains(pattern, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
