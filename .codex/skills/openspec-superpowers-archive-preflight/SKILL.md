---
name: openspec-superpowers-archive-preflight
description: Use before archiving an OpenSpec change to check task completion, validation evidence, documentation sync, review records, phase state, and residual risk.
---

# OpenSpec Superpowers Archive Preflight

## Overview

用于 OpenSpec change 归档前检查。核心原则：先证明变更可归档，再归档；如果存在明确阻塞，必须如实报告，不伪造测试、手动验收或工具执行结果。

## When To Use

- 用户要求归档当前阶段或指定 `openspec/changes/<change-id>`。
- 用户要求归档前检查 `tasks.md`、spec 同步、文档更新、未解决风险。
- 用户要求按 Superpowers 流程完成 OpenSpec archive。

## Required Context

开始前读取：

1. `AGENTS.md`
2. `GOALS.md`
3. `PHASES.md`
4. `.ai/workflow.md`
5. `openspec/config.yaml`
6. `openspec/changes/<change-id>/proposal.md`
7. `openspec/changes/<change-id>/design.md`
8. `openspec/changes/<change-id>/tasks.md`
9. `openspec/changes/<change-id>/specs/**/*.md`

## Workflow

1. **确认变更**
   - 如果用户指定了 change id，使用该 id。
   - 如果未指定，运行 `openspec list`，不要猜测当前 change。
   - 如果存在多个 active changes，先报告冲突并请求用户指定。

2. **检查 tasks**
   - 统计 `tasks.md` 中 `- [x]` 和 `- [ ]`。
   - 勾选统计必须写入最终报告和归档摘要。
   - 未完成任务必须有明确阻塞原因或残留风险说明。
   - 工具不可用、测试无法执行、环境缺失必须写成阻塞或残留风险，不允许改写为已完成。

3. **检查 spec 同步**
   - 归档前运行 `openspec validate --changes --strict`。
   - 确认 change 下的 spec 能通过 OpenSpec 校验。
   - 归档后运行 `openspec validate --specs --strict`。
   - 归档后检查 `openspec/specs/**/spec.md`，不得保留无意义占位内容。

4. **检查文档与阶段状态**
   - 检查 `GOALS.md`、`PHASES.md`、`.ai/workflow.md` 是否需要随功能状态更新。
   - 检查是否存在 `.ai/documents/reviews/<change-id>-subagent-review.md`；如果该 change 不需要 subagent，最终报告必须说明不适用原因。
   - 需要时生成或更新 `.ai/documents/archive-reports/<change-id>-archive-summary.md`，记录 tasks 统计、验证命令、文档同步、审查记录和残留风险。

5. **检查未解决风险**
   - 汇总阻塞任务、未跑测试、手动验收缺口、OpenSpec 警告。
   - 风险必须区分“阻塞归档”和“归档后残留风险”。

6. **执行归档**
   - 使用 OpenSpec CLI：`openspec archive <change-id> --yes`。
   - 不手动移动目录，除非 CLI 不可用且用户明确确认。

7. **归档后验证**
   - 运行 `openspec list`，确认 active changes 状态。
   - 检查 `openspec/changes/archive/` 下归档目录存在。
   - 运行 `openspec validate --specs --strict`。
   - 复查归档摘要、subagent 审查记录、`PHASES.md` 和 `GOALS.md` 同步状态。

## Final Report

用中文报告：

- 归档结果和归档目录。
- `tasks.md` 完成状态；如有阻塞，列出任务编号和原因。
- spec 同步结果和通过的验证命令。
- 文档是否需要更新。
- `PHASES.md` / `GOALS.md` 是否已同步。
- 归档摘要和 subagent 审查记录路径。
- 未解决风险。
- 本次修改过或新增的文件。

## Common Mistakes

| 错误 | 正确做法 |
| --- | --- |
| 把阻塞验收标成完成 | 保持阻塞状态并在最终报告说明 |
| 只运行规格校验但不检查 tasks | tasks、spec、文档、阶段状态都要检查 |
| 手动搬目录代替 CLI | 优先使用 `openspec archive <change-id> --yes` |
| 归档后不验证主规格 | 必须运行 `openspec validate --specs --strict` |
| 归档后不更新阶段状态 | 必须同步 `PHASES.md` 和 `GOALS.md` |
| 审查证据只散落在对话里 | 写入 `.ai/documents/reviews/<change-id>-subagent-review.md` |
