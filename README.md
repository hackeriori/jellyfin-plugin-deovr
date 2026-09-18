# 可在 *DeoVR* 中直接浏览媒体库并播放 VR 视频的 *Jellyfin* 插件

> [!CAUTION]
> 如果您的 Jellyfin 服务器暴露在公网或非信任网络中，**务必**在插件设置中配置 **IP 限制（IP Restrictions）**！

---

## 📌 项目来源

本项目基于开源项目 [Toastyice/DeoVRDeeplink](https://github.com/Toastyice/DeoVRDeeplink) 进行二次开发与升级改造。感谢原作者 [Toastyice](https://github.com/Toastyice) 的出色工作！

## 🌟 主要特性

- **DeoVR 内置浏览**：在 VR 头显内的 DeoVR 播放器中直接浏览 Jellyfin 媒体库。
- **一键“在 DeoVR 中播放”**：在 Jellyfin Web 网页端播放界面中集成“Play in DeoVR”快捷按钮，一键调起播放。
- **时间线缩略图预览**：支持后台自动生成时间线缩略图，在 DeoVR 中悬浮在进度条即可实时预览画面。
- **智能 VR 视频格式检测**：自动识别 2D、传统 3D 巨幕、VR180、VR360、180°/220° 鱼眼、佳能 RF5.2mm、MKX200 鱼眼以及左右 (SBS) / 上下 (TB) / CUV 画面布局。
- **安全的 HMAC 签名直链**：为每个媒体流生成临时签名代理链接，过期自动失效，保障媒体资源安全。
- **IP 白名单访问控制**：支持通过 CIDR 格式指定允许访问 DeoVR 接口的 IP 范围。
- **完美兼容 DeoVR 官方 JSON 规范**：无缝契合 [DeoVR API 规范](https://deovr.com/app/doc)。

---

## 🖼️ 界面预览

![example](Images/example.png "example")

---

## 🛠️ 安装与准备

### 前置要求

- [Jellyfin Media Server](https://jellyfin.org/)（需要配置有效的 **HTTPS 证书**，DeoVR 客户端要求通过 HTTPS 连接才能正常加载媒体流）。
- 已安装 [DeoVR 客户端](https://deovr.com/app)（支持 Meta Quest、PICO、PCVR、Apple Vision Pro 等各平台）。

### 安装步骤

1. 打开 Jellyfin 管理控制台（控制台 → 插件 → 代码库）。
2. 添加本插件的代码库清单（Repository Manifest）：
   ```text
   https://raw.githubusercontent.com/hackeriori/jellyfin-plugin-deovr/main/manifest.json
   ```
3. 在“目录”中找到 **JellyfinPluginDeoVR** 插件并点击安装。
4. 重启 Jellyfin 服务器以使插件生效。

---

## ⚙️ 插件配置

在 Jellyfin 控制台菜单中进入 **控制台 → 插件 → JellyfinPluginDeoVR** 进行配置：

- **IP 限制 (IP Restriction)**：
  - 支持通过 CIDR 格式（如 `192.168.1.0/24`, `10.0.0.0/8`, `127.0.0.1/32`）设置允许访问 DeoVR 接口的 IP 范围，提升服务器安全性。
- **媒体库设置 (Library Settings)**：
  - **启用 (Enabled)**：开启或关闭指定媒体库在 DeoVR 中的浏览与展示。
  - **排序依据与顺序 (Sort By / Sort Order)**：按名称、随机、添加时间或上映日期进行升序/降序排列。
  - **时间线缩略图 (Timeline Images)**：为该媒体库的视频生成快进快退悬停预览图。
  - **强制投影 / 强制立体模式 (Forced Projection / Forced Stereo Mode)**：强制该媒体库内的所有视频使用特定的投影或立体模式，覆盖文件名和元数据识别（适合专门的“VR180 专区”或“3D 电影专区”）。
  - **保底投影 / 保底立体模式 (Fallback Projection / Fallback Stereo Mode)**：当视频未检测到任何标签、文件名标记或 3D 元数据时所使用的默认模式。

---

## 📐 视频格式识别与 VR 命名规范

插件内置了智能格式探测器，能够自动区分传统的平面 3D 电影、VR 等距柱状投影（180°/360°）、鱼眼镜头格式以及常规 2D 视频。

### 识别优先级层级

格式判断按以下从高到低的优先级顺序进行解析：
1. **项目元数据标签**：在 Jellyfin 中为视频编辑填写的标签。
2. **媒体库强制配置**：在插件设置中为整个媒体库指定的强制投影/立体模式。
3. **文件名命名规范**：根据视频文件名自动正则解析。
4. **Jellyfin 原生 3D 元数据**：Jellyfin 扫描到的 `Video3DFormat`（如标准 `.3D.hsbs` 压制片默认映射为平面 3D 巨幕，不会变形）。
5. **媒体库保底配置**：在插件设置中为媒体库指定的保底模式。
6. **全局默认**：常规 2D 平面视频（`screenType: flat`, `stereoMode: off`, `is3d: false`）。

---

### 方式一：文件名自动识别（推荐，省心快捷）

只需在文件名中包含标准的关键字标识即可。标签**不区分大小写**，可出现在文件名的任何位置，使用 `_`、`-`、`.`、空格或括号分隔。

#### 1. 投影类型 / 屏幕类型 (Projection / Screen Types)

| DeoVR 投影类型 | 格式说明 | 支持的文件名关键字 |
| :--- | :--- | :--- |
| `flat` | 标准 2D 平面或 3D 巨幕影院模式 | `flat`, `cinema`, `flat3d`, 或带 Jellyfin `.3D.` 且无 VR 标记的文件 |
| `dome` | 180° 等距柱状投影 (VR180) | `_180`, `180`, `vr180`, `180vr`, `lr_180`, `180_lr` |
| `sphere` | 360° 全景等距柱状投影 (VR360) | `_360`, `360`, `vr360`, `360vr` |
| `fisheye` | 180° / 220° 鱼眼镜头 | `_fisheye`, `fisheye`, `fisheye180`, `vrca220` |
| `fisheye` *(优化)* | 190° 鱼眼镜头 (佳能 RF 5.2mm) | `_fisheye190`, `fisheye190`, `rf52`, `_rf52` *(实测映射为 fisheye 防畸变)* |
| `mkx200` | 200° MKX 鱼眼镜头 | `_mkx200`, `mkx200`, `fisheye200`, `_fisheye200` |

> [!NOTE]
> 文件名解析采用了严格的单词与数字边界判定，因此类似 `1080p`、`2160p`、`360p` 的分辨率标识**绝不会**误触发 `180` 或 `360` 的投影匹配。

#### 2. 左右 / 上下立体布局 (Stereo Layouts)

| 立体模式 | 布局说明 | 支持的文件名关键字 |
| :--- | :--- | :--- |
| `sbs` | 左右半宽 / 全宽 3D (Side-by-Side) | `sbs`, `hsbs`, `fsbs`, `lr`, `3dh`, `sidebyside`, `side-by-side`, `half-sbs`, `full-sbs` |
| `tb` | 上下半高 / 全高 3D (Top-and-Bottom) | `tb`, `htab`, `ftab`, `ou`, `3dv`, `overunder`, `topbottom`, `half-ou`, `full-ou` |
| `cuv` | 自定义 UV 格式 (佳能 RF 5.2mm 原始双鱼眼输入) | `cuv` |
| `off` | 单目 2D / 单目 VR (Monoscopic 2D) | `2d`, `mono`, `monoscopic` |

#### 3. 文件命名示例

- `VR_Video_SBS_180.mp4` → 180° 穹顶 (Dome), 左右 3D (SBS)
- `Nature_360_TB.mp4` → 360° 全景球幕 (Sphere), 上下 3D (TB)
- `Space360_Mono.mp4` → 360° 全景球幕 (Sphere), 单目 2D
- `Rollercoaster_SBS_fisheye.mp4` → 180° 鱼眼, 左右 3D
- `Sample_RF52_SBS.mp4` → 佳能 RF5.2mm 鱼眼, 左右 3D (自动采用优化后的 fisheye 网格渲染)
- `My_video_SBS_mkx200.mp4` → 200° MKX 鱼眼, 左右 3D
- `Avatar (2009).3D.HSBS.1080p.mkv` → 平面 3D 巨幕（保持左右 3D 效果，不会被错误扭曲成球体！）
- `Titanic (1997).3D.HTAB.1080p.mkv` → 平面 3D 巨幕（上下 3D）
- `Inception (2010) 1080p.mkv` → 普通 2D 平面影片

---

### 方式二：Jellyfin 元数据标签（无需重命名文件）

如果您无法修改文件名（例如 PT 做种或依赖严格刮削的文件），可以直接在 Jellyfin 中打标签覆盖：

1. 在 Jellyfin 网页端中，点击视频卡片上的 **`...`（更多选项）**，选择 **修改元数据**。
2. 找到 **标签** 字段，添加相应的标签：
   - **投影类型标签**：`VR180`（或 `dome`）、`VR360`（或 `sphere`）、`Fisheye`、`Fisheye190`（或 `RF52`）、`MKX200`、`Flat`
   - **立体模式标签**：`SBS`、`TB`、`2D`（或 `Mono`）、`CUV`
   - **带前缀标签（可选）**：`deovr:dome`、`deovr:sphere`、`deovr:fisheye`、`deovr:rf52`、`deovr:mkx200`、`deovr:sbs`、`deovr:tb`、`deovr:off`
3. 保存即可。元数据标签的优先级高于文件名识别。

---

### 方式三：专用媒体库集中设置

如果您的媒体库按类型分类（例如单独建了“VR 180”或“3D 蓝光电影”媒体库）：
1. 进入 **控制台 → 插件 → JellyfinPluginDeoVR**。
2. 找到对应的媒体库，直接设置 **强制投影（Forced Projection）** 和/或 **强制立体模式（Forced Stereo Mode）**。
3. 该库下的所有视频将直接采用该模式播放，无需逐个重命名或打标签。

---

## 🎮 使用方法 (Usage)

1. **直接在 DeoVR 中浏览**：
   - 启动 VR 头显中的 DeoVR 播放器。
   - 在地址栏输入您的 Jellyfin 服务器完整地址（例如 `https://jellyfin.example.com`）。
   - DeoVR 将以原生 VR 卡片界面的形式展现您开启了 DeoVR 支持的媒体库。
2. **在 Jellyfin 网页端一键直达**：
   - 在 Jellyfin Web 播放页面中，点击右上角的 **“Play in DeoVR”** 按钮即可一键调起 DeoVR 播放器。

---

## 🔒 安全性

- **HMAC 临时签名令牌**：所有视频流请求链接均受动态 HMAC 密钥签名保护。
- **防盗链与防篡改**：链接具有防重放与过期限制，过期后自动失效。
- **服务端私钥保护**：签名密钥存储于服务器端，绝不泄露给客户端。
- **自适应有效时长**：视频流签名的有效期限动态设置为电影时长的两倍。
- **CIDR IP 白名单**：除前端注入脚本和图标外，所有 API 接口均支持 IP 白名单访问过滤。

---

## 💻 编译与开发 (Development)

本项目使用 .NET 10 SDK 进行开发与构建：

```bash
# 克隆本仓库
git clone https://github.com/hackeriori/jellyfin-plugin-deovr.git
cd jellyfin-plugin-deovr

# 还原依赖并编译发布
dotnet restore
dotnet build -c Release
```

欢迎提交 Issue 和 Pull Request！

---

## ❓ 常见问题与故障排查

### 1. Jellyfin 网页端看不到“Play in DeoVR”按钮

这通常是因为 Jellyfin 服务器对 `index.html` 文件的写入权限不足，导致插件无法自动注入前端挂载脚本。

#### 1.1 Linux / Docker 容器环境修复
如果您的 Jellyfin 运行在 Docker 容器中，请使用以下命令修改 `index.html` 的所有权并重启容器（请将 `jellyfin` 替换为您的实际容器名，`user:group` 替换为您容器内的运行用户与组，通常是 `1000:1000` 或 `root:root`）：

```bash
docker exec -it --user root jellyfin chown user:group /jellyfin/jellyfin-web/index.html && docker restart jellyfin
```

*(可在系统开机或容器启动时设置相关权限)*

#### 1.2 Windows 原生安装环境修复
1. 打开资源管理器，进入目录：`C:\Program Files\Jellyfin\Server\jellyfin-web\`
2. 右键点击 `index.html` 文件 → 选择 **属性** → 切换到 **安全** 选项卡 → 点击 **编辑**。
3. 从用户列表中选择当前运行 Jellyfin 的用户，勾选 **写入（Write）** 权限。
4. 重启 Jellyfin 服务并刷新浏览器页面。

### 2. 视频在 DeoVR 中无法加载或无法开始播放

- **检查 HTTPS 与 SSL 证书**：DeoVR 客户端有严格的安全要求，**必须**使用有效的 HTTPS 证书（不支持自签名不受信任证书）。
- **检查反向代理配置**：如果使用了 Nginx、Caddy、Traefik 等反向代理，请确保正确透传了 `Host`、`X-Forwarded-For`、`X-Forwarded-Proto` 等请求头，且代理未限制大文件流式传输与超时时间。

---

## 💖 鸣谢与致敬

- 本项目由 [Toastyice/DeoVRDeeplink](https://github.com/Toastyice/DeoVRDeeplink) 衍生并深度改造升级而来，在此向原作者致以崇高的敬意！
- [Jellyfin Media Server](https://jellyfin.org/) - 优秀的开源流媒体服务器。
- [DeoVR](https://deovr.com/) - 极致体验的专业 VR 播放器。

---

**祝您享受绝妙的沉浸式观影体验！🎉**
