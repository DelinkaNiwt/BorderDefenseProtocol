分析BDP模组弹道系统的"'管线"架构和"追踪"、"引导"两个模块之间的组成情况和相互关系。
  我已经调试这两个模块很久了，总是有各种bug，包括但不限于命中如何判定，会不会穿墙，路径上碰撞判定等  
  等各种问题，改了1个问题，其它问题又出现了。
  我感觉在这不对劲，在我的预想里，这两个模块的核心任务就1件事：告诉子弹我接下来该往哪飞
  即使再有的任务也是为这个核心任务而服务的
  其它的和该核心任务无关的东西那都该是子弹自己或者其它模块的事

  说白了，子弹原本就知道如何从自己A直直飞向目标B，知道路上碰上其它东西了该怎么办，知道飞到目标位置了 
  该不该中，中了该如何。这些是子弹自己就该知道的。

  引导模块和追踪模块只是让子弹不飞得那么直而已。
  引导模块就是设法在开枪前就预设几个中转点，告诉子弹要先直飞第一个点A，再直直地飞向第二个点B，如此反 
  复，直到最后直直地飞向目标Z（可能是一个实体，也可能只是一个格子一个坐标)。无论如何它始终就只是在飞 
  而已。先飞向多个不同的位置，然后在最后一个中转点朝目标开枪，最后中没中，关我什么事。
  追踪模块比引导模块简单，子弹飞出后本来是直飞，引导模块则是按照规则或算法，时刻告诉子弹要往哪个方向 
  各偏一点。最后追不追的上，追上了中没中，也不关我的事。

  实际可能也没有我说的这么简单，但是现在我们总是在和模块无关的地方打补丁，我都不知道这套系统打了多少 
  厚重无用的补丁了。我预想的架构不该是这样子的。

  重新审视当前的代码实现，按照我说的痛点去深入探究，找出当前架构问题和功能实现问题。都有哪些，是什   
  么，造成了什么影响，尤其是连锁影响。

  先不干别的，就干这件事。

  -----

   诊断报告：BDP弹道系统架构问题                                                                                                                                                                           
  你的直觉是对的                                                                                                                                                                                            你说的核心理念完全正确：追踪模块和引导模块的唯一职责就是"告诉子弹下一步往哪飞"。但现在这两个模块里   塞了大量不属于它们的东西。我逐一拆解。                                                             

  ---                                          
  问题1：TrackingModule 越权管理"命中判定"     

  TrackingModule 里有三处直接操纵 usedTarget：

  - ResolvePath 第198-210行：finalApproach 时 host.usedTarget = host.TrackingTarget
  - HandleArrival 第346-354行：distToTarget < 1f 时 host.usedTarget = host.TrackingTarget

  这是在做什么？追踪模块在替子弹决定"你该命中谁"。

  按你的理念，追踪模块只管"往哪偏"，子弹飞到目标位置后中不中、中谁，那是 vanilla 的 ImpactSomething  
  的事。但因为 vanilla 的 usedTarget 机制（无视距离直接命中），追踪弹如果不手动同步
  usedTarget，就会出现"飞过目标后凭空命中原始目标"的幽灵伤害。

  连锁影响：为了修这个问题，又在 Bullet_BDP.ImpactSomething 第378-389行加了 TrackingExpired
  补丁——追踪过期时强制 Impact(null)
  打地面。这个补丁本身又引入了新问题：如果追踪弹恰好在目标附近过期，本该命中的也被强制打地面了。     

  根因：追踪模块被迫承担了"修正 vanilla 命中判定缺陷"的职责。

  ---
  问题2：TrackingModule 越权管理"生命周期"（自毁）

  OnTick 第264-272行和第292-304行：超时自毁、丢锁自毁。

  追踪模块在决定子弹什么时候该死。这不是"告诉子弹往哪飞"的事。子弹飞出地图边界、超时、丢失目标后该怎 
  么办，应该是子弹自己或者一个专门的生命周期管理器的事。

  连锁影响：hadTrackingLock + trackingLostTicks + lostTrackingSelfDestructTicks
  这套状态机完全是为了防止"丢锁后子弹残留触发 vanilla
  拦截"而打的补丁。追踪模块的状态变量从"角度+角速度"膨胀到了9个字段。

  ---
  问题3：RedirectFlightTracking 里的 origin 后退 hack

  Bullet_BDP 第162-200行的 RedirectFlightTracking，以及第126-146行的 RedirectFlightGuided，都有一个  
  ORIGIN_OFFSET = 6f 的 hack——把 origin 往后退6格。

  这是为什么？因为 vanilla 的 InterceptChanceFactorFromDistance 用 origin
  到拦截格的距离来计算拦截概率，距离 ≤ 5格时返回0（不拦截）。追踪弹每 tick 重置 origin =
  当前位置，导致 origin 到前方墙壁永远只有1-2格，墙壁拦截完全失效，子弹穿墙。

  这是追踪模块的问题吗？不是。 这是"每 tick 重定向弹道"这个行为与 vanilla
  拦截系统的冲突。但修复被塞进了 Bullet_BDP 的重定向方法里，而且是用一个魔法数字 6f 硬编码的。       

  连锁影响：这个 hack 改变了 origin 的语义——origin
  不再是"子弹实际出发点"，而是"一个为了让拦截公式正常工作的虚拟点"。任何依赖 origin
  做计算的代码（包括 vanilla 的 ExactPosition 插值）都可能受影响。

  ---
  问题4：GuidedModule 和 TrackingModule 通过 IsOnFinalSegment 隐式耦合

  TrackingModule 第105行：if (!host.IsOnFinalSegment) return;

  追踪模块必须等引导模块把 IsOnFinalSegment 设为 true 才能激活。这个协作协议是通过一个宿主上的 bool  
  字段隐式传递的，没有任何接口约束。

  连锁影响：
  - GuidedModule 在 SetWaypoints 时设 IsOnFinalSegment = false，在 HandleArrival 最后一个锚点时设    
  true
  - TrackingModule 在 ResolvePath 里检查这个字段
  - 如果执行顺序出问题（比如 Priority 配错），追踪永远不激活或者在引导段就激活
  - postLaunchInitDone 那段代码（Bullet_BDP 第290-305行）就是为了修复"SpawnSetup 时
  FinalTarget/TrackingTarget 还没初始化"的时序问题

  ---
  问题5：GuidedVerbState 是一个巨大的"胶水层"

  GuidedVerbState 有290行代码，管理了：
  - 锚点数据存储
  - LOS 检查重定向
  - TryStartCastOn 拦截（单侧/双侧两套）
  - PostCastOn 处理（单侧/双侧两套）
  - 自动绕行路由的准备和分配
  - 双武器系统的左右侧状态

  这个类本质上是在 Verb 层和弹道层之间做大量的"翻译"工作。它知道太多东西了——它知道 LOS 怎么检查、知道
   currentTarget 要锁定为 Cell、知道自动绕行怎么分配。

  连锁影响：每次 Verb 层有变化（比如新增 ComboShoot），都要在 GuidedVerbState 里加对应的处理分支。   

  ---
  问题6：RedirectFlightTracking 的远/近距离双策略

  第176-198行，远距离用固定60tick，近距离用实际距离。这是为了解决"每 tick CeilToInt
  取整导致速度累积误差"的问题。

  但这个修复本身又引入了新的边界条件：speedPerTick * 3f 和 speedPerTick * 1.5f 两个阈值，加上        
  finalApproach 标志，形成了一个三段式状态机（远距离→近距离→极近距离）。每个阈值的选择都是经验值，换 
  一个弹速就可能出问题。

  ---
  总结：问题的本质

  你的系统有一个设计良好的管线架构（v4），接口定义清晰。但在实现层面，追踪模块和引导模块承担了三类不 
  属于它们的职责：

  ┌───────────────────────────────┬───────────────────┬────────────────────────────┐
  │           越权职责            │     应该在哪      │          现在在哪          │
  ├───────────────────────────────┼───────────────────┼────────────────────────────┤
  │ 命中判定（usedTarget同步）    │ 子弹/Impact层     │ TrackingModule             │
  ├───────────────────────────────┼───────────────────┼────────────────────────────┤
  │ 生命周期管理（自毁）          │ 子弹/生命周期模块 │ TrackingModule             │
  ├───────────────────────────────┼───────────────────┼────────────────────────────┤
  │ vanilla拦截修正（origin后退） │ 子弹/拦截适配层   │ RedirectFlight方法         │
  ├───────────────────────────────┼───────────────────┼────────────────────────────┤
  │ 追踪过期处理                  │ 子弹/Impact层     │ Bullet_BDP.ImpactSomething │
  ├───────────────────────────────┼───────────────────┼────────────────────────────┤
  │ Verb→弹道的数据翻译           │ 更薄的适配层      │ GuidedVerbState（290行）   │
  └───────────────────────────────┴───────────────────┴────────────────────────────┘

  这些越权职责之间还互相依赖，形成了一张补丁网：
  origin后退hack ← 每tick重定向 ← 追踪模块
      ↓
  穿墙修复 → 但改变了origin语义 → 影响ExactPosition插值
      ↓
  usedTarget同步 ← 追踪模块 ← 为了防止幽灵命中
      ↓
  TrackingExpired ← Bullet_BDP ← 为了防止usedTarget同步后的反效果
      ↓
  丢锁自毁 ← TrackingModule ← 为了防止残留子弹触发vanilla拦截

  每个补丁都是为了修前一个补丁的副作用。这就是你说的"改了1个问题，其它问题又出现了"的根源。

  — Claude Opus 4.6

  ----

   你这个直觉是对的。当前实现里，“引导/追踪只负责告诉子弹下一步飞哪”这条边界已经被打破，模块正在介入命  中结果、生命周期和引擎拦截模型，所以才会出现“修一个点，别处冒 bug”。                               
                                                                                                     
  核心结论                                                                                           
                                                                                                     
  1. 现在不是“纯导航管线”，而是“导航 + 结果补丁 + 时序补丁”的混合体。                                
     证据：Bullet_BDP.cs:282 Bullet_BDP.cs:377 TrackingModule.cs:261                                 
  2. 引导和追踪不是解耦关系，而是通过 FinalTarget/TrackingTarget/IsOnFinalSegment 强耦合共享状态。   
     证据：GuidedModule.cs:79 GuidedModule.cs:83 TrackingModule.cs:103                               
  3. 你现在遇到的命中/穿墙/路径碰撞问题，本质上都和“频繁重写 origin/destination/ticksToImpact”以及   
     usedTarget 语义漂移有关。                                                                       
     证据：Bullet_BDP.cs:162 GuidedVerbState.cs:130 Projectile.cs:490                                
                                                                                                     
  “管线”实际运行关系（现在）                                                                         
                                                                                                     
  1. 模块由 Def 扩展注册并按 Priority 排序，Guided=10，Tracking=15。                                 
     证据：BDPMod.cs:26 GuidedModule.cs:23 TrackingModule.cs:72                                      
  2. Tick 阶段：先 PathResolver，再 base.TickInterval（原版飞行+拦截），再 PositionModifier，最后    
     TickObserver。                                                                                  
     证据：Bullet_BDP.cs:306 Bullet_BDP.cs:319 Projectile.cs:238                                     
  3. Arrival 阶段：按排序先 Guided 再 Tracking，谁把 ctx.Continue=true 就短路后续。                  
     证据：Bullet_BDP.cs:365 Bullet_BDP.cs:368 GuidedModule.cs:62                                    
                                                                                                     
  架构问题（关键）                                                                                   
                                                                                                     
  1. 结果层泄漏进导航层：TrackingModule 提供强制同步 usedTarget 的选项；Bullet_BDP 用 TrackingExpired
     改写 ImpactSomething。这已经不是“只给方向”。                                                    
     证据：TrackingModule.cs:199 TrackingModule.cs:342 BDPTrackingConfig.cs:69 Bullet_BDP.cs:377     
  2. 管线契约被绕开：IBDPPathResolver 名义上改 Destination，但 Tracking 直接改宿主 origin/           
     ticksToImpact。IBDPTickObserver 文档是观察者，但 Tracking 在里边销毁子弹。                      
     证据：IBDPPathResolver.cs:27 TrackingModule.cs:211 IBDPTickObserver.cs:4 TrackingModule.cs:267  
  3. 时序修补导致多源真相：OnSpawn 时机拿不到稳定目标，后面靠 postLaunchInitDone 补；Guided 又必须额 
     外同步 TrackingTarget 防失活。                                                                  
     证据：Bullet_BDP.cs:289 GuidedModule.cs:80 Verb_BDPRangedBase.cs:259 GuidedVerbState.cs:165     
                                                                                                     
  你关心的三个痛点，对应问题                                                                         
                                                                                                     
  1. 命中判定：引导态会把 currentTarget 锁成 Cell，导致 usedTarget 常常不是 Thing，原版会回退到概率命     中分支；于是又引入“强制 usedTarget”补丁。                                                       
     证据：GuidedVerbState.cs:130 Verb_BDPRangedBase.cs:244 Projectile.cs:490                        
  2. 会不会穿墙/拦截：原版拦截概率依赖 origin 距离，距离平方 <=25 时直接 0；你们每 tick 重定向让     
     origin 贴近子弹，所以才出现 offset=6 的结构性补丁。                                             
     证据：VerbUtility.cs:147 Projectile.cs:271 Bullet_BDP.cs:171                                    
  3. 路径碰撞判定：Arrival 阶段不断 Continue 重定向 + 丢锁自毁，会改变“何时/是否进入原版 Impact 流   
     程”，副作用扩散到碰撞与命中体验。                                                               
     证据：TrackingModule.cs:356 TrackingModule.cs:287 Bullet_BDP.cs:360                             
                                                                                                     
  连锁影响（为什么总是补丁互炸）                                                                     
                                                                                                     
  1. 修“命中不稳” -> 强制 usedTarget -> 导航模块介入结果层 -> 与掩体/姿态/随机分支语义冲突。         
  2. 修“穿墙/拦截失效” -> 改 origin 偏移 -> 影响飞行插值和到达时机 -> 又要补 Arrival 近距逻辑。      
  3. 修“初始化错误目标” -> 加 postLaunchInit -> 再要求 Guided 手动同步 TrackingTarget -> 新模块再写目
     标就可能复发。                                                                                  
  4. 修“丢锁后鬼伤/乱拦截” -> 自毁或地面 Impact -> 近目标看起来像“明明追上却没中”。                  
                                                                                                     
  补一条你会关心的实现偏差：Argus 注释写 trackingDelay=0，实际配置是 3，这会让“末段追踪”再延迟几个   
  tick。                                                                                             
  证据：ThingDefs_Projectiles.xml:154 ThingDefs_Projectiles.xml:185                                  
                                                                                                     
  以上就是当前架构和实现层面的问题画像。你的判断“引导/追踪应只负责下一步飞向哪里”与现状偏差点，已经可
  以明确定位。