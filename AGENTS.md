# CoddLoom 协作规范

基于 ADO.NET 的轻量、显式 ORM：`netstandard2.0` 核心包，加上每个数据库各自的 provider 包。
没有 LINQ provider，没有变更跟踪器，没有隐式的工作单元。

## 维护方式

- 本文件是仓库内 AI 协作流程与约束的唯一事实来源。
- Codex 直接读取本文件；Claude Code 通过根目录 `CLAUDE.md` 导入本文件。
- 修改通用规范时只改本文件。除非某条规则确实只适用于单个工具，否则不要把规则正文写进
  `CLAUDE.md` 或其他工具入口文件，以免内容漂移或互相冲突。

## 文档语言

- **仓库内的流程与约束文档用中文**：本文件、`.github/ISSUE_TEMPLATE/` 下的 issue 模板、
  `.github/pull_request_template.md`。
- **提交到 GitHub 的 issue 和 PR，正文一律用中文。**
  issue 标题用中文；PR 标题沿用英文 conventional commit 格式（`feat:` / `fix:` / `docs:` …），
  因为 squash 合并后它就是 `main` 上那条 commit 的标题。
- **review 全程用中文**：发到 PR 上的行内意见、review summary、回复，以及在对话里向我汇报
  review 发现的问题，一律中文。引用的代码、标识符、编译器诊断码（如 `CS0618`）、命令行照原样保留。
- **面向使用者的文档用英文**：`README.md`、`THIRD-PARTY-NOTICES.md`、代码里的 XML 文档注释、
  异常消息、`[Obsolete]` 等特性中面向调用方的文字。这些是公开发布物的一部分。
- 代码标识符、commit message 保持英文。

## 范围纪律

这个仓库有一个反复出现的失败模式：一个范围很窄、描述很清楚的 issue，做成 PR 之后进入
review，来回很多轮都收敛不了。每次的机理都是同一个——review 在紧挨着 diff 的代码里发现了
一个真实的既有缺陷，这个缺陷在同一个 PR 里被顺手修掉，而这次修复又产生了新的可 review 面
（行为变化、兼容性说明、文档、发布说明），于是循环重复。

最严重的一次见 [PR #34](https://github.com/philfanzhou/CoddLoom/pull/34)：issue #24 明确
排除了任何运行时行为变化，但它的七条 review 意见里有三条是 `DbEngine.Extension.cs` 中的
既有缺陷。其中一条还是在 PR 内被修掉了（`068da65b`，culture-invariant 的时间 ID），而这一个
越界 commit 又自己产生了第七条意见——已持久化 ID 的兼容性、README、发布说明，也就是又一轮。
对照 [PR #33](https://github.com/philfanzhou/CoddLoom/pull/33)：同类的既有缺陷被推迟到
issue #40，PR 直接合掉，没有把它吸收进来。

下面的规则就是为了打断这个循环。它们不是风格偏好。

### 在宣布一个 issue 可以开工之前

绝不能只看 issue 描述就判断它 ready。先把 issue 里提到的每个文件、每个成员从头到尾读一遍。

在那些代码里发现的、issue 并没有要求修的缺陷，都属于**邻近债务**。每一条：单独开一个 issue，
然后在目标 issue 的 `## 已知邻近问题（本次不修）` 一节里链接过去。

一个 issue 只有同时满足三条才算 ready：

1. 它自己写清楚了 `## 最小修改范围` 和 `## 验收标准`。
2. 它将要改动的代码已经被读过。
3. 那段代码里的邻近债务已经开成 issue 并链接好。

「描述很清楚」不等于 ready。一个清楚的 issue 架在一段烂代码上，恰恰就是会失控的那种情况。

### 实施过程中

issue 的 `最小修改范围` 有约束力。issue 说不会变的运行时行为就不要动——哪怕你确实、可验证地
发现那里是坏的，也不要顺手改。开 issue 记下来。

### review 过程中

每条意见在写下来之前先分类：

- **本 PR 引入的**——缺陷位于本 PR 新增或修改的行上。在本 PR 里修。
- **既有的**——缺陷位于本 PR 只是移动、重新缩进、改名波及、或者仅仅是挨着的代码里。
  开一个 issue，在 review 意见里链接过去，并明确说明它不在本 PR 范围内。不要在本 PR 里修。

只有一个例外：某个既有缺陷导致本 PR 自己的 `验收标准` 无法验证。援引这个例外时，必须指名
是哪一条验收标准。

意见真实、可复现、证据充分，并不等于它在范围内。范围由 PR 改动了哪些行决定，不由意见质量决定。

### 熔断线

一个 PR 进入第三轮 review 时，停下来，先不要再写代码，把它的 commit 逐个对照 issue 的
`最小修改范围`。任何一个追溯不到某条验收标准的 commit 都是范围违规：撤掉它，改为开 issue。

## 合并 PR 后

PR 合并不等于工作结束。合并后必须检查并合理处理以下事项，不要遗留失效状态或无主分支：

1. 确认远端 PR 已合并，目标分支已经包含合并结果。合并时用了 `gh pr merge --delete-branch`
   的话，注意本地删分支那一步可能因为下面第 4 条的 worktree 占用而失败；PR 本身仍然会正常
   合并，照第 4–6 条手工收尾即可。
2. 检查 PR 对应的远端 issue。已经完成的 issue 应关闭；只完成一部分或仍有后续工作的 issue
   应保持开启，并更新说明或链接后续 issue，不能因为 PR 已合并就放任状态失真。PR 本来就没有
   关联 issue 时（例如维护者直接要求的改动），在汇报里写明「无关联 issue」，不要跳过这一条。
3. 确认远端工作分支已经随合并自动删除（仓库开启了 `deleteBranchOnMerge`）。分支仍然存在时，
   要么删掉，要么说明保留原因。
4. 用 `git worktree list` 检查残留 worktree，删掉不再需要的，并执行 `git worktree prune`。
   `.claude/worktrees/` 下的工作区被 `.git/info/exclude` 忽略，不会出现在 `git status` 里，
   是最容易漏掉的那类失效状态。这一步排在更新目标分支之前：只要目标分支还被某个 worktree
   检出，第 5 条就做不了。删之前先确认那个工作区是干净的、没有独有提交。
5. 更新本地的目标分支：`git switch main && git merge --ff-only origin/main`。如果 `main` 仍然
   被别的工作区检出，`git switch main` 和 `git fetch origin main:main` 都会直接失败——前者报
   `already checked out at ...`，后者报 `refusing to fetch into branch ...`，两条命令谁也绕不
   过去。只能先按第 4 条移除那个工作区，或者进到那个工作区里更新并在汇报中说明。
6. 清理本地工作分支和远端跟踪引用。本仓库用 squash 合并，工作分支的 commit 不会成为目标分支
   的祖先，因此 `git branch -d` 一定报 `not fully merged`，`git branch --merged` 也一定列不出
   它——这不构成「还没合并」的证据。删除前用别的方式确认改动确实进去了（`gh pr view <编号>
   --json state,mergeCommit` 显示已合并，或者该分支与目标分支之间已经没有实质 diff），确认之后
   再 `git branch -D`。确认不了就保留分支并说明原因；绝不要因为 `-d` 失败就顺手换成 `-D`。
7. 向用户汇报 PR、issue、远端分支、本地分支和 worktree 各自的最终状态；任何未完成的清理都要
   说明原因和后续动作。

## 已知债务热点

`src/CoddLoom/DbEngine.Extension.cs` —— 客户端的 `Generate*Id` 系列在一轮 review 里就产出了
issue #36、#37、#38、#39。任何要动这个文件的排期，做邻近债务盘点时格外小心。
