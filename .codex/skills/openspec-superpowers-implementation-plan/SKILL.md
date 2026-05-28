---
name: openspec-superpowers-implementation-plan
description: Use when generating a Chinese implementation plan for an approved OpenSpec change, including task order, file scope, acceptance, parallelism, and tests without writing code.
---

# OpenSpec Superpowers Implementation Plan

## Overview

用于根据当前 OpenSpec change、`tasks.md` 和审查报告生成中文实施计划。核心原则：计划必须能直接指导后续小步实现，但本流程只写文档，不写业务代码。

## Required Context

开始前读取：

1. `AGENTS.md`
2. `PHASES.md`
3. `GOALS.md`
4. `.ai/workflow.md`
5. `openspec/changes/<change-id>/proposal.md`
6. `openspec/changes/<change-id>/design.md`
7. `openspec/changes/<change-id>/tasks.md`
8. `openspec/changes/<change-id>/specs/**/*.md`
9. 最近一次 `.ai/documents/reviews/<change-id>-subagent-review.md`，如存在

## Output Location

优先保存到：

- `.ai/documents/plans/<change-id>-implementation-plan.md`

如果用户指定其他位置，以用户指定为准。

## Workflow

1. 确认当前阶段和 change id。
2. 读取项目规则、阶段文件和 OpenSpec 文档。
3. 扫描现有代码目录，确认计划中的路径符合项目分层。
4. 按 `tasks.md` 原顺序生成计划，不重排任务编号。
5. 每个任务必须写清：
   - 串行、可并行或收尾串行。
   - 是否需要测试。
   - 要修改的文件或目录，最多 3 项。
   - 可观察验收方式。
   - 依赖条件或阻塞条件。
6. 把 subagent 审查风险落实到计划中。
7. 最后补充：
   - 串并行建议。
   - 测试任务汇总。
   - 阶段完成标准。

## Forbidden

- 不写业务代码。
- 不修改无关文件。
- 不跳过 `tasks.md` 顺序。
- 不让单个任务超过 3 个文件范围。
- 不把“写测试”作为笼统验收，必须写清测试覆盖什么。
- 不引入源项目功能内容。

## Validation

完成后检查：

- 计划中每个 `tasks.md` 条目都有对应步骤。
- 每步文件范围不超过 3 项。
- 每步都有验收方式。
- 每步都有串并行和测试标记。
- 文档无未完成占位词。
