---
name: openspec-superpowers-goal-execution
description: Use when executing the current GOALS.md objective through an approved OpenSpec implementation plan or active OpenSpec tasks.
---

# OpenSpec Superpowers Goal Execution

## Overview

用于按 Superpowers 流程执行当前 OpenSpec 阶段目标。核心规则：优先读取 `openspec-superpowers-implementation-plan` 生成的具体实施计划，并按计划连续执行到完成；每个检查点都要更新 `tasks.md`，并记录修改文件、验证结果和阻塞项。

## Execution Contract

当 `.ai/documents/plans/<change-id>-implementation-plan.md` 存在时，本技能进入“实施计划连续执行模式”：

- 必须以实施计划为主执行源，按计划覆盖的任务顺序全部执行。
- 不在每个检查点后询问用户“是否继续”“下一步做什么”或要求用户确认下一步。
- 不把“请用户确认下一步”作为正常流程的一部分。
- 除非遇到真实阻塞，否则持续推进到计划任务全部完成、验证完成、或当前环境无法继续。
- 真实阻塞仅包括：需求互相冲突、缺少无法替代的用户输入、工具/Unity/编译环境不可用且没有可执行替代验证、或继续执行会覆盖用户未授权改动。
- 遇到非阻塞验证缺口时，记录不可执行原因，继续完成其他可执行任务。
- 可以向用户发送简短进度更新，但更新不是暂停点，也不是确认请求。
- 最终报告只在连续执行结束后输出。

如果实施计划不存在，先尝试根据当前 OpenSpec change 生成实施计划；只有在无法生成计划或 change 不明确时，才请求用户补充信息。

## Required Context

开始前读取：

1. `AGENTS.md`
2. `PHASES.md`
3. `GOALS.md`
4. `.ai/workflow.md`
5. 当前 `openspec/changes/<change-id>/proposal.md`
6. 当前 `openspec/changes/<change-id>/design.md`
7. 当前 `openspec/changes/<change-id>/tasks.md`
8. 当前 `openspec/changes/<change-id>/specs/**/*.md`
9. 当前 `.ai/documents/plans/<change-id>-implementation-plan.md`，如存在

## Workflow

1. 确认当前阶段和 change id。
2. 运行 `openspec list`，确认 active change 状态。
3. 检查 SVN 状态，识别已有用户改动；不要覆盖无关修改。
4. 读取实施计划；如没有计划，先生成实施计划。只有 change id 或需求不明确时才请求用户补充信息。
5. 按实施计划覆盖的 `tasks.md` 顺序拆成小检查点，并从第一个未完成检查点开始连续执行。
6. 每个检查点执行，不等待用户确认：
   - 写或更新必要测试。
   - 实现最小功能。
   - 运行可用验证。
   - 更新 `tasks.md` 勾选完成项，或写明阻塞原因。
   - 汇总本检查点修改文件。
7. 检查点完成后立即进入下一个未完成检查点；禁止把“下一步”交还给用户决定。
8. 如果测试工具、Editor 环境或外部工具不可用：
   - 不伪造测试或手动验收结果。
   - 使用可用的编译式验证、OpenSpec 校验或静态检查。
   - 在 `tasks.md` 或最终报告中明确不可执行原因。
   - 继续执行不依赖该工具的其他计划任务。
9. 收尾执行：
   - 更新必要文档。
   - 运行 `openspec validate --changes --strict`。
   - 运行可用测试或编译检查。
   - 最终报告完成项、阻塞项、验证命令和修改文件。

## State Sync

- 当前目标写在 `GOALS.md`。
- 阶段状态写在 `PHASES.md`。
- 任务完成状态写在 `openspec/changes/<change-id>/tasks.md`。
- 审查结论写在 `.ai/documents/reviews/`。
- 实施计划写在 `.ai/documents/plans/`。

## Forbidden

- 不修改无关文件。
- 不跳过 `tasks.md`。
- 不创建重复状态源。
- 不把历史任务堆进 `GOALS.md`。
- 不伪造测试、工具输出或手动验收结果。
- 不在失败验证后声称完成。
- 实施计划存在时，不在检查点之间要求用户确认下一步。
- 实施计划存在时，不把未执行完的计划拆成多轮让用户反复发送“下一步”。
- 不因为任务很多就提前停止；只能在计划完成或真实阻塞时停止。

## Progress Reporting

执行中可以报告：

- 当前检查点。
- 正在修改的文件范围。
- 已完成的验证。
- 发现的真实阻塞。

执行中不要报告：

- “是否继续？”
- “下一步要不要做？”
- “请确认我继续执行计划。”

只有当继续执行需要用户做不可替代决策时，才提出具体问题；问题必须说明阻塞点和可选决策的影响。

## Final Report

最终用中文报告：

- 当前阶段完成状态。
- 已完成任务范围。
- 阻塞任务及原因。
- 关键修改文件。
- 验证命令和结果。
- 残留风险和下一步。
