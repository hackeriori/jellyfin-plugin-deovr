# DeoVR 应用开发文档 (DeoVR App Documentation)

> **原文来源**：[https://deovr.com/documentation](https://deovr.com/documentation)  
> **文档说明**：本文档为 DeoVR 官方开发文档的中文翻译与整理版，涵盖了服务器配置、视频编码与解码器、单/多视频深度链接（Deeplink）规范、图片 Feed、远程控制 TCP 协议、空间音频（Spatial Audio）、投影网格命名规范、直播流（HLS/RTSP）、Passthrough 透视与 Alpha 抠像通道、字幕及支持的文件格式等核心内容。

---

## 目录 (Table of Contents)

1. [如何配置服务器 (How to configure servers)](#1-如何配置服务器-how-to-configure-servers)
2. [视频编码基础 (Video encoding basics)](#2-视频编码基础-video-encoding-basics)
3. [播放视频编解码器 (Playback video codecs)](#3-播放视频编解码器-playback-video-codecs)
4. [集成开发介绍 (Intro to the integration)](#4-集成开发介绍-intro-to-the-integration)
5. [单视频深度链接格式规范 (Single video deeplink formatting)](#5-单视频深度链接格式规范-single-video-deeplink-formatting)
6. [多视频选择深度链接 / 选择场景 (Multiple videos selection deeplink / Selection Scene)](#6-多视频选择深度链接--选择场景-multiple-videos-selection-deeplink--selection-scene)
7. [图片支持 (Images Support)](#7-图片支持-images-support)
8. [远程控制 (Remote control)](#8-远程控制-remote-control)
9. [空间音频 (Spatial Audio)](#9-空间音频-spatial-audio)
10. [支持投影网格的命名规范 (Naming convention for supported meshes)](#10-支持投影网格的命名规范-naming-convention-for-supported-meshes)
11. [HLS、RTMP、RTSP 直播流 (HLS, RTMP, RTSP)](#11-hlsrtmprtsp-直播流-hls-rtmp-rtsp)
12. [红蓝 3D 模式 (Anaglyph mode)](#12-红蓝-3d-模式-anaglyph-mode)
13. [透视模式 / 混合现实 (Passthrough mode)](#13-透视模式--混合现实-passthrough-mode)
14. [鱼眼视频 Alpha 通道透视 (13.9+) (Alpha channel passthrough for Fisheye videos)](#14-鱼眼视频-alpha-通道透视-139-alpha-channel-passthrough-for-fisheye-videos)
15. [字幕 (Subtitles)](#15-字幕-subtitles)
16. [支持的本地文件格式与编解码器 (Supported local file formats and codecs)](#16-支持的本地文件格式与编解码器-supported-local-file-formats-and-codecs)
17. [获取 DeoVR 客户端 (Get the DeoVR App)](#17-获取-deovr-客户端-get-the-deovr-app)

---

## 1. 如何配置服务器 (How to configure servers)

进行视频点播（VoD）流媒体传输最简单的方式是使用**渐进式 HTTP 流（Progressive HTTP Streaming）**，这种方式无需任何额外或特殊的 Web 服务器配置。

DeoVR 播放器会先下载视频文件头（Header），并根据这些信息向 Web 服务器发送 **HTTP Range 请求**，以便按需分段下载视频文件的必要部分。

- **必须使用 HTTPS**：以确保与所有 Android 版本兼容。
- **配置要求**：只需将视频文件上传到 Web 服务器的公开目录下，只要视频编码正确，无需任何额外的服务器配置即可正常播放。

**该方式存在两个缺点：**
1. 无法根据用户的网速实现无缝画质自适应切换。
2. 用户可以通过直接下载链接下载您的视频文件。

---

## 2. 视频编码基础 (Video encoding basics)

> *待完善（TBC）。官方近期将更新和扩充此章节。*

在此之前，您可以阅读关于新一代 AV1 编解码器的官方博客：[关于全新 AV1 编解码器介绍 (AV1 Video Blog)](https://deovr.com/blog/77-av1-video)。

---

## 3. 播放视频编解码器 (Playback video codecs)

在 Windows 操作系统上，必须安装正确的视频编解码器才能使 DeoVR 正常播放。  
DeoVR 客户端播放器支持以下任意一种编解码器：

- [LAVFilters](https://github.com/Nevcairiel/LAVFilters/releases)
- [HEVC Video Extension (HEVC 视频扩展)](https://codecguide.com/media_foundation_codecs.htm)

请确保系统中至少安装了其中之一。

> **注意**：[Quest 3S](https://deovr.com/blog/162-meta-quest-3s-and-deovr) / [Quest 3](https://deovr.com/blog/75-vr-videos-on-the-meta-quest-3) 头显原生支持全新的 [AV1 编解码器](https://deovr.com/blog/77-av1-video)。官方目前正在评估 DeoVR Windows 应用对 AV1 的支持情况，后续将发布相关更新。

---

## 4. 集成开发介绍 (Intro to the integration)

将 DeoVR 集成到您的网站中主要有两种方式：

### A. 单视频深度链接 (Single video deeplink)
在服务器上上传包含单个视频描述的 `.json` 文件，然后在网站页面中添加一个调用按钮。用户点击该深度链接按钮后，DeoVR 将启动并直接播放该 JSON 文件中定义的视频，视频会立即开始播放。

### B. 多视频选择 (Multiple videos selection)
1. **通过 DeoVR 浏览器调用**：  
   在服务器上上传包含视频列表的单个 `.json` 文件。用户在 DeoVR 的内置“网络浏览器（Internet browser）”中访问该网站（例如 `www.deovr.com`），即可进入“选择场景（Selection Scene）”，在列表中挑选想看的视频。
2. **通过网站上的深度链接调用**：  
   在服务器上上传包含视频列表的单个 `.json` 文件。然后在网站中添加一个指向该文件的深度链接按钮（例如 `deovr://https://www.yoursite.com/something.json`）。用户在普通浏览器中打开网站并点击该按钮后，将唤起 DeoVR 并直接展示选择场景界面。

> **提示**：您可以单独实现上述其中一种集成方式，也可以两者同时实现。

---

## 5. 单视频深度链接格式规范 (Single video deeplink formatting)

> **重要提示 (IMPORTANT!)**  
> DeoVR 会在本地内部保存每个视频的播放设置。默认情况下，视频标题（`title`）用于区分视频。但如果您希望发送多个具有相同标题的视频，请务必使用 `id` 字段来进行唯一区分：
> ```json
> "title": "ExampleVideo",
> "id": 555
> ```

### 1. 分辨率列表 (Resolutions)
创建 `videoSources` 数组，为每个视频源设置播放 URL 和对应的垂直分辨率（`resolution`）：

```json
"videoSources": [
    {
        "resolution": 1080,
        "url": "https://yoursite.com/JsonExampleVideos/ExampleVideo_1080p.mp4"
    },
    {
        "resolution": 1440,
        "url": "https://yoursite.com/JsonExampleVideos/ExampleVideo_1440p.mp4"
    }
]
```

### 2. 视频时间轴拖动预览 (Video preview / Seek LookUp)
您可以提供一个视频文件，用于在播放器进度条拖动（快进/快退）时显示悬停缩略预览：

```json
"videoThumbnail": "https://yoursite.com/ExampleVideo_SeekLookUp.mp4"
```

> **注意**：该视频建议采用低分辨率和低帧率（Low FPS），以节约系统资源。在移动端 DeoVR 上该功能可能不可用。

### 3. 选择场景中的必备缩略图与鼠标悬停预览 (Thumbnail & Preview)
如果是从列表（Selection Scene）中打开或展示该视频，需要配置以下字段：

```json
"videoPreview": "https://yoursite.com/ExampleVideo_Preview.mp4",
"thumbnailUrl": "https://yoursite.com/ExampleVideo_Thumbnail.jpg"
```

- `videoPreview`（可选）：指向视频预览短片的链接，当用户在列表中将光标悬停该视频卡片时播放。
- `thumbnailUrl`（必须）：列表中显示的封面静态缩略图地址。若在选择场景中使用，此项为必填字段。

### 4. 时间戳 / 章节打点 (Timestamps)
创建 `timeStamps` 列表，每个时间戳对象包含秒数 `ts` 和标记名称 `name`：

```json
"timeStamps": [
    {
        "ts": 15,
        "name": "Rabbit jumps"
    },
    {
        "ts": 30,
        "name": "Rabbit Sleeps"
    }
]
```

> **重要提示**：为了使 `timeStamps` 正常工作，您必须同时在 JSON 中指定以秒为单位的视频总时长 `videoLength`，否则打点功能无法生效：
> ```json
> "videoLength": 60
> ```

### 5. 画面校正参数 (Corrections)
通过 `corrections` 字典可声明默认的画面校正参数：
- `x`：水平偏移量，取值范围 `[-7.5, 7.5]`
- `y`：垂直偏移量，取值范围 `[-7.5, 7.5]`
- `br`：亮度（Brightness），取值范围 `[-70, 70]`
- `cont`：对比度（Contrast），取值范围 `[-70, 70]`
- `sat`：饱和度（Saturation），取值范围 `[-70, 70]`

```json
"corrections": {
    "x": 5,
    "y": -5,
    "br": -10,
    "cont": 10,
    "sat": 20
}
```

### 6. 其它核心参数 (Other settings)
- **立体声模式 (`stereoMode`)**：
  - `"sbs"`：左右分屏立体 3D（Side by Side）
  - `"tb"`：上下分屏立体 3D（Top-Bottom）
  - `"cuv"`：自定义 UV 布局（Custom UV layout，目前仅用于佳能 RF5.2mm 镜头）
  - `"off"`：单目 2D（Monoscopic）
- **投影网格类型 (`screenType`)**：
  - `"flat"`：普通平面 2D 视频
  - `"dome"`：180° 等距柱状投影（Equirectangular 180° mesh）
  - `"sphere"`：360° 全景等距柱状投影（Equirectangular 360° mesh）
  - `"fisheye"`：180° 鱼眼投影（Fisheye 180° mesh）
  - `"mkx200"`：200° 鱼眼镜头校正投影（MKX 200° mesh）
  - `"rf52"`：190° 佳能鱼眼镜片校正投影（Canon RF5.2mm fisheye mesh）
- **3D 开关 (`is3d`)**：
  > **重要提示**：若为 3D 视频，`is3d` 应始终设为 `true`。若设为 `false`，视频将被强制以单目（2D）模式播放。

### 7. 单视频完整 JSON 示例 (Full JSON example)

```json
{
  "encodings": [
    {
      "name": "h264",
      "videoSources": [
        {
          "resolution": 1080,
          "url": "https://yoursite.com/ExampleVideo_1080p.mp4"
        },
        {
          "resolution": 1440,
          "url": "https://yoursite.com/ExampleVideo_1440p.mp4"
        },
        {
          "resolution": 1920,
          "url": "https://yoursite.com/ExampleVideo_1920p.mp4"
        },
        {
          "resolution": 2160,
          "url": "https://yoursite.com/ExampleVideo_2160p.mp4"
        },
        {
          "resolution": 2880,
          "url": "https://yoursite.com/ExampleVideo_2880p.mp4"
        },
        {
          "resolution": 3360,
          "url": "https://yoursite.com/ExampleVideo_3360p.mp4"
        },
        {
          "resolution": 3840,
          "url": "https://yoursite.com/ExampleVideo_3840p.mp4"
        }
      ]
    }
  ],
  "title": "ExampleVideo",
  "id": 123,
  "videoLength": 60,
  "is3d": true,
  "screenType": "sphere",
  "stereoMode": "tb",
  "skipIntro": 0,
  "videoThumbnail": "https://yoursite.com/ExampleVideo_SeekLookUp.mp4",
  "videoPreview": "https://yoursite.com/ExampleVideo_Preview.mp4",
  "thumbnailUrl": "https://yoursite.com/ExampleVideo_image.jpg",
  "timeStamps": [
    {
      "ts": 15,
      "name": "Wall"
    },
    {
      "ts": 30,
      "name": "Window"
    },
    {
      "ts": 45,
      "name": "Door"
    }
  ],
  "corrections": {
    "x": 5,
    "y": -5,
    "br": -10,
    "cont": 10,
    "sat": 20
  }
}
```

---

## 6. 多视频选择深度链接 / 选择场景 (Multiple videos selection deeplink / Selection Scene)

多视频 JSON 文件包含单视频描述对象的集合。您可以定义多个场景（`scenes`），这些场景将在 DeoVR 选择场景（Selection Scene）界面的下方以标签页（Tab）的形式呈现。

JSON 文件必须至少包含一个场景。每个场景对应一个视频列表。

### 基础场景结构示例

```json
{
   "scenes": [
      {
         "name": "Trailers",
         "list": [
            /* 此处为单视频描述对象，与单视频 deeplink 结构一致 */
         ]
      },
      {
         "name": "Full Videos",
         "list": [
            /* 此处为单视频描述对象，与单视频 deeplink 结构一致 */
         ]
      }
   ],
   "authorized": "0"
}
```
此时客户端界面将生成两个名为 `Trailers` 和 `Full Videos` 的标签页。

### 单场景包含两部视频的完整示例

```json
{
  "scenes": [
    {
      "name": "Library",
      "list": [
        {
          "encodings": [
            {
              "name": "h264",
              "videoSources": [
                {
                  "resolution": 1080,
                  "url": "https://yoursite.com/ExampleVideo1_1080p.mp4"
                },
                {
                  "resolution": 1440,
                  "url": "https://yoursite.com/ExampleVideo1_1440p.mp4"
                }
              ]
            }
          ],
          "title": "ExampleVideo1",
          "screenType": "sphere",
          "stereoMode": "tb",
          "skipIntro": 0,
          "videoThumbnail": "https://yoursite.com/ExampleVideo1_SeekLookUp.mp4",
          "videoPreview": "https://yoursite.com/ExampleVideo1_Preview.mp4",
          "thumbnailUrl": "https://yoursite.com/ExampleVideo1_image.jpg",
          "timeStamps": [
            {
              "ts": 15,
              "name": "Wall"
            }
          ],
          "corrections": {
            "x": 5,
            "y": -5,
            "br": -10,
            "cont": 10,
            "sat": 20
          },
          "is3d": true,
          "videoLength": 60,
          "id": 123
        },
        {
          "encodings": [
            {
              "name": "h264",
              "videoSources": [
                {
                  "resolution": 1080,
                  "url": "https://yoursite.com/ExampleVideo2_1080p.mp4"
                },
                {
                  "resolution": 1440,
                  "url": "https://yoursite.com/ExampleVideo2_1440p.mp4"
                }
              ]
            }
          ],
          "title": "ExampleVideo2",
          "screenType": "sphere",
          "stereoMode": "tb",
          "skipIntro": 0,
          "videoThumbnail": "https://yoursite.com/ExampleVideo2_SeekLookUp.mp4",
          "videoPreview": "https://yoursite.com/ExampleVideo2_Preview.mp4",
          "thumbnailUrl": "https://yoursite.com/ExampleVideo2_image.jpg",
          "timeStamps": [
            {
              "ts": 15,
              "name": "Wall"
            }
          ],
          "corrections": {
            "x": 3,
            "y": -3,
            "br": -5,
            "cont": 5,
            "sat": 10
          },
          "is3d": true,
          "videoLength": 65,
          "id": 234
        }
      ]
    }
  ],
  "authorized": "0"
}
```

### 简化格式 (Shortened format / 延迟加载)
如果视频列表数据量过大，可以使用**简化格式**。  
此时无需直接在列表中返回播放视频所需的完整参数，仅需提供以下 4 个字段，并附带一个指向各视频完整描述 JSON 接口的 `video_url` 字段：

- `thumbnailUrl`：视频封面缩略图图片地址；
- `title`：视频标题；
- `videoLength`：视频时长（以秒为单位）；
- `video_url`：返回该视频完整详细 JSON 描述（见第 5 节单视频格式）的接口地址。

```json
{
  "scenes": [
    {
      "name": "Library",
      "list": [
        {
          "title": "Play with a pretty dog",
          "videoLength": 79,
          "thumbnailUrl": "https://deovr.com/s/images/feed/thumb1.png",
          "video_url": "https://deovr.com/deovr/video/id/1"
        },
        {
          "title": "Bikini car wash",
          "videoLength": 242,
          "thumbnailUrl": "https://deovr.com/s/images/feed/thumb2.png",
          "video_url": "https://deovr.com/deovr/video/id/2"
        },
        {
          "title": "Date with a girl",
          "videoLength": 401,
          "thumbnailUrl": "https://deovr.com/s/images/feed/thumb3.png",
          "video_url": "https://deovr.com/deovr/video/id/2"
        }
      ]
    }
  ]
}
```

### DeoVR 访问文件名与路由规范 (Choosing of .json file name)
- 如果您希望在 DeoVR 内置浏览器中直接输入形如 `http://www.yoursite.com` 的根域名进行访问，请在 Web 服务器的根目录下放置一个名为 `deovr` 的文件（**无扩展名**，且无引号）。
- 当访问只包含域名的链接时，DeoVR 会自动向 `http://www.yoursite.com/deovr` 请求数据。
- 如果请求带路径的完整 URL（例如 `http://www.yoursite.com/video/test`），DeoVR 会直接原样请求该地址。服务器返回的内容必须是符合规范的包含视频描述列表的 JSON 数据。

### DeoVR 用户认证与登录机制 (Authorization in DeoVR)
当涉及登录验证时，服务器响应的 JSON 根对象中应包含 `authorized` 字段，其含义如下：
- `1`：用户已成功通过认证（已登录）；
- `0`：未登录访客（无账号模式）；
- `-1`：认证失败（账号或密码错误）。

当认证失败（返回 `-1`）时，DeoVR 客户端会弹出提示：*«Invalid login or password!»*（用户名或密码无效！）。如果站点不需要认证，该字段可省略或保持为 `0`。

**认证流程：**
- 当用户在 DeoVR 客户端中尝试登录时，当前 JSON 链接（无论是列表链接还是视频详情链接）将通过 **POST 请求**方法重新发起请求。
- 用户输入的账号和密码将分别通过请求表单字段 `login` 和 `password` 提交给服务端。
- 服务端校验完成后，将认证结果体现在前述的 `authorized` 字段中返回。
- 认证生效后，后续获取场景列表及视频详情的所有请求中，均会附带 `login` 与 `password` 参数。

---

## 7. 图片支持 (Images Support)

为了向用户提供图片流 Feed（而非视频），需要使用 `path` 字段传递图片文件的完整网络链接（替代 `encodings` 编码类）。  
对于 `stereoMode`（立体模式）和 `screenType`（投影类型），您可以显式在 JSON 中定义参数，也可以直接在图片文件名中包含特定的命名标签。

### 图片 Feed JSON 示例

```json
{
  "scenes": [
    {
      "name": "Library",
      "list": [
        {
          "path": "https://yoursite.com/picture1_tb_360.jpg",
          "title": "ExamplePicture1",
          "screenType": "sphere",
          "stereoMode": "tb",
          "thumbnailUrl": "https://yoursite.com/thumbnail_picture1.jpg",
          "corrections": {
            "x": 5,
            "y": -5,
            "br": -10,
            "cont": 10,
            "sat": 20
          },
          "is3d": true,
          "id": 123
        },
        {
          "path": "https://yoursite.com/picture2_sbs_360.jpg",
          "title": "ExamplePicture2",
          "screenType": "sphere",
          "stereoMode": "sbs",
          "thumbnailUrl": "https://yoursite.com/thumbnail_picture2.jpg",
          "is3d": true,
          "videoLength": 65,
          "id": 234
        }
      ]
    }
  ],
  "authorized": "0"
}
```

---

## 8. 远程控制 (Remote control)

DeoVR 支持通过 TCP 协议进行网络远程控制。官方提供了一个用 C# Windows Forms 编写的简易远程控制客户端源码与示例程序：

- 客户端下载地址：[DeoRemoteControlTest.zip](https://deovr.com/s/DeoRemoteControlTest.zip)

### 1. 准备 DeoVR 播放器
1. 启动 DeoVR 应用程序。
2. 在选择场景或文件浏览器中打开设置菜单（右上角“齿轮”图标）。
3. 勾选并开启 **"Enable remote control" (启用远程控制)** 开关。
4. Windows 系统可能会弹出防火墙提示，请允许传入的网络连接。
5. 从选择场景或本地浏览器中播放任意视频。

### 2. 准备远程控制客户端
1. 解压并运行 `DeoRemoteControlTest` 程序。
2. 如果控制端与 DeoVR 在同一台 PC 上运行，点击 **“Connect”** 即可；如果不在同一台设备上，请先填入运行 DeoVR 的设备 IP 地址。
3. 建立连接后，将提示 **“Client connected”**，随后将开始实时接收来自 DeoVR 的状态广播。

**处于播放状态时，控制端可接收到：**
- 当前播放视频文件的路径 (`path`)
- 播放器当前状态 (`playerState`，播放或暂停)
- 当前播放时间 (`currentTime`)
- 当前播放倍速 (`playbackSpeed`)

**控制端可向 DeoVR 发送的控制指令：**
- 新的视频路径（DeoVR 将立即打开并播放新视频）
- 跳转目标时间 `SeekTo`（定位至指定播放秒数）
- 播放倍速调整（修改当前播放速度）
- 也可以在同一个数据包中同时传递多个参数。

### 3. 代码集成说明
该示例应用程序源码采用 C# 编写，允许免费自由使用。在独立程序中可复用 `RemoteControlClient.cs` 类进行通信，其封装了：
- 方法：`Connect`、`Disconnect`、`Send`
- 事件：`OnConnected`、`OnDisconnected`、`OnDataReceived`

在视频播放期间，`OnDataReceived` 事件每 1 秒触发一次。`OnDataReceived` 和 `Send` 均操作 `RemoteApiData` 数据实体类。具体填充和发送方式可参考 `TestForm.cs` 中的按钮点击逻辑。

### 4. DeoVR 原始 TCP 远程控制协议规范 (Raw TCP Protocol)
远程控制客户端通过 **TCP 端口 23554** 连接至运行 DeoVR 的设备。

- **通信频次**：连接建立后，DeoVR 会以每秒 1 次的频率向客户端发送当前状态包；远程客户端也必须每秒向 DeoVR 发送一次心跳数据包（空包或包含 JSON 的包），用于保活 Ping。
- **超时机制**：如果 DeoVR 超过 **3 秒**未收到任何来自客户端的数据包，将主动断开 TCP 连接。
- **数据包结构 (Packet structure)**：
  - 每个数据包均以一个 **4 字节整型数值（32-bit Integer，UTF-8 字符数据的字节长度）** 作为包头。
  - 若长度为 `0`，表示没有视频数据载荷，即为空心跳包（用于 Ping）。
  - 若长度大于 0，其后紧跟相应长度的 UTF-8 编码 JSON 字符串。

**载荷 JSON 格式：**
```json
{
    "path": "D:/test.mp4",
    "duration": 123.45,
    "currentTime": 10.5,
    "playbackSpeed": 1.0,
    "playerState": 0
}
```
- `playerState` 枚举定义：
  - `0`：正在播放 (Play)
  - `1`：已暂停 (Pause)
- **控制指令**：控制端可以向 DeoVR 发送包含 `path`、`currentTime` 和 `playbackSpeed` 字段的数据包。DeoVR 会自动打开新视频/路径、跳转到指定播放时刻或调整播放速度。唯一的前提条件是：**DeoVR 必须当前处于视频播放器界面内**。

---

## 9. 空间音频 (Spatial Audio)

DeoVR 已加入对空间音频（Spatial Audio）的支持：
- **容器与编码规范**：最终 VR 视频必须封装在 `.mkv` 容器中，音频需使用 **Opus** 编解码器。
- **音频格式**：目前仅支持 **Facebook 360 `tbe8_2` (TBE 8.2)** 格式。
- **制作工具**：了解更多制作流程请访问 [Spatial Workstation 官方文档](https://facebookincubator.github.io/facebook-360-spatial-workstation/Documentation/SpatialWorkstation/SpatialWorkstation.html#encoder)。从 Spatial Workstation 导出时，请选择 **FB360 Matroska (experimental)**。

**官方空间音频测试视频下载：**
- [Deo_spatial_test_1_FB360_SBS_180.mkv](https://s3.deovr.com/misc/Deo_spatial_test_1_SBS_180_FB360.mkv)
- [Deo_spatial_test_2_FB360_SBS_180.mkv](https://s3.deovr.com/misc/Deo_spatial_test_2_SBS_180_FB360.mkv)

### 深度链接配置空间音频
若要通过 DeoVR 深度链接在网站上启用空间音频，请在 JSON 编码中添加独立的 `encodings_spatial` 字段。该字段结构与 `encodings` 类似，但 URL 需直接指向只包含空间音频轨的 `.mkv` 文件。  
> **重要提示**：该 `.mkv` 文件中**严禁包含普通立体声音轨**。

- 若使用 `path` 方式直接播放文件而非 `encodings`，文件名中必须带有 `"-FB360"` 或 `"_FB360"` 标识。
- 目前客户端暂不支持手动在不同音轨之间切换。

### 上传带有空间音频的母带文件规范
当向平台上传带有空间音频的视频时，母带文件必须满足：
1. 母带必须采用 **MKV** 格式，并包含两条音轨。
2. **音轨 1 (Track 1)**：必须包含 Facebook 360 TBE 8.2 格式的空间音频轨。
3. **音轨 2 (Track 2)**：必须存在，且为双声道常规立体声（2-channel stereo）格式。

### 空间音频 JSON 配置示例

```json
{
  "scenes": [
    {
      "name": "Library",
      "list": [
        {
          "encodings": [
            {
              "name": "h264",
              "videoSources": [
                {
                  "resolution": 1080,
                  "url": "https://yoursite.com/ExampleVideo_1080p.mp4"
                },
                {
                  "resolution": 1440,
                  "url": "https://yoursite.com/ExampleVideo_1440p.mp4"
                }
              ]
            }
          ],
          "encodings_spatial": [
            {
              "name": "h264",
              "videoSources": [
                {
                  "resolution": 1080,
                  "url": "https://yoursite.com/SpatialExampleVideo_1080p.mkv"
                },
                {
                  "resolution": 1440,
                  "url": "https://yoursite.com/SpatialExampleVideo_1440p.mkv"
                }
              ]
            }
          ],
          "title": "ExampleVideo1_Spatial",
          "screenType": "sphere",
          "stereoMode": "tb",
          "skipIntro": 0,
          "videoThumbnail": "https://yoursite.com/ExampleVideo1_SeekLookUp.mp4",
          "videoPreview": "https://yoursite.com/ExampleVideo1_Preview.mp4",
          "thumbnailUrl": "https://yoursite.com/ExampleVideo1_image.jpg",
          "timeStamps": [
            {
              "ts": 15,
              "name": "Wall"
            }
          ],
          "is3d": true,
          "videoLength": 60,
          "id": 123
        }
      ]
    }
  ],
  "authorized": "0"
}
```

---

## 10. 支持投影网格的命名规范 (Naming convention for supported meshes)

DeoVR 原生支持以下投影类型（网格 Meshes）：
- **平面 2D (Flat 2D plane)**
- **等距柱状投影 (Equirectangular)**：
  - 180° 视场角 (180° FOV)
  - 360° 视场角 (360° FOV)
- **鱼眼投影 (Fisheye projection)**：
  - 180° 视场角 (180° FOV)
  - 190° 视场角 (190° FOV)
  - 200° 视场角（配备 MKX 镜头校正）
  - 220° 视场角（配备 VRCA 镜头校正）

> **注意：视频朝向的正确性对沉浸式 VR 体验至关重要：**  
> - **后期制作**：务必在后期剪辑过程中确定好视频的初始朝向，以确保保留创作者预期的视角。  
> - **应用内调整**：DeoVR 应用内支持通过偏航角（Yaw）进行 ±180 度的微调，以校正相机方向。但对于重大朝向变动，不建议仅依赖应用内调整。

### 双眼排布 (Eye location)
- 左右排布：**Side-by-Side**
- 上下排布：**Top-Bottom**

### 本地文件命名标签规则
在播放本地文件时，只需在文件名中加入以下标识，DeoVR 即可自动以正确的模式渲染播放：

#### 投影格式标识（等距柱状/鱼眼）：
- `_180`：180° 等距柱状投影
- `_360`：360° 等距柱状投影
- `_fisheye`：180° 鱼眼投影
- `_fisheye190`：190° 鱼眼投影（如佳能 Canon VR 镜头）
- `_mkx200`：200° MKX 镜头校正鱼眼
- `_vrca220`：220° VRCA 镜头校正鱼眼

#### 立体格式排布标识：
- 左右分屏（Side by Side）：`LR` 或 `3DH` 或 `SBS`
- 上下分屏（Top Bottom）：`TB` 或 `3DV` 或 `OverUnder`

#### Alpha 通道透视标识：
- `_alpha`：标记该视频包含打包的 Alpha 通道。DeoVR 播放本地文件时会自动检测并启用，上传至 DeoVR Drive 时也会自动识别，无需手动切换开关。（详见第 14 节说明）。

> **特点**：所有文件名标签**不区分大小写**，且可以以**任意顺序**出现在文件名中。

#### 文件命名示例：
- `My_video_SBS_alpha_mkx200.mp4`：左右分屏 + 200° 鱼眼 + Alpha 透视通道
- `My_video_SBS_mkx200_ALPHA.mp4`：同上（大小写不敏感）
- `Title_SBS_180.mp4`：左右分屏 + 180° 全景
- `My_video_SBS_mkx200.mp4`：左右分屏 + 200° 鱼眼
- `Title_FB360_SBS_180.mkv`：左右分屏 + 180° 全景 + FB360 空间音频

---

## 11. HLS、RTMP、RTSP 直播流 (HLS, RTMP, RTSP)

关于直播推流，官方提供了两篇入门博客，推荐先行参阅：
- [使用 OBS 进行 VR 视频直播教程](https://deovr.com/blog/112-tutorial-how-to-live-stream-vr-video-using-obs)
- [在 DeoVR 上使用 HLS 进行 VR 视频直播](https://deovr.com/blog/73-live-streaming-vr-video-at-deovr)

### 树莓派 + Zcam K2 Pro 相机直播方案
您可以借助树莓派（Raspberry Pi）将 Zcam K2 Pro 专业相机的画面直接串流至 Quest 头显。这让导演或创作者可以在 VR 中获得实时的导演监视器画面，在按下录制按钮前微调布景，甚至可以边录制边监看。

**操作步骤：**
1. 准备树莓派，刷入配套系统镜像，并使用原装充电器或充足功率的充电宝供电。将树莓派远离路由器等强 WiFi 干扰源。
2. 主相机（带屏幕和录制按钮的机身 A）负责推流，输出约 5Mbps 码率的单目画面，延迟约 10 秒。
3. 确保 Quest 头显中安装了 DeoVR。
4. 打开相机，在菜单中选择 `Connect > Network > ETH`，设置为 `Direct`。
5. 启动树莓派，使用网线将树莓派连接至 K2 Pro 相机，直播推流将自动启动。
6. 将 Quest 头显连接至树莓派发出的 WiFi 热点 `RPI4_Camera_GW`。
7. 在 DeoVR 播放器中打开内置浏览器，将协议切换为 `http://`（点击 `https://` 即可切换）。
8. 在浏览器地址栏输入 `10.0.0.1`（即 `http://10.0.0.1`），按回车键，然后点击 **“Start Stream”**。画面会有约 10 秒缓冲延迟。
9. 若相机画面颠倒，可在播放器设置中反转：进入 `Settings > Image` 标签，将 `Rotation` 滑块调整为 ±180 度。
10. 在 VR 沉浸视角中检视画面并进行布光调教。

### 协议支持情况总结
- **RTSP**：仅在 **Windows 平台** 上可用。
- **RTMP**：**不支持**。
- **HLS**：**全面支持**（跨平台）。

### 在 DeoVR 中播放自定义 HLS 流
- 直接在 DeoVR 浏览器地址栏中输入 `.m3u8` 流地址即可打开。
- 或者在服务器上提供名为 `deovr` 的 JSON 配置文件，配置如下：

```json
{
    "scenes": [
        {
            "name": "VR180 Samples",
            "list": [
                {
                    "path": "https://yoursite.com/Example.m3u8",
                    "title": "ExampleStream",
                    "screenType": "sphere",
                    "stereoMode": "sbs",
                    "thumbnailUrl": "https://yoursite.com/ExampleTumb.jpg",
                    "is3d": true,
                    "id": "01"
                }
            ]
        }
    ],
    "authorized": "0"
}
```

---

## 12. 红蓝 3D 模式 (Anaglyph mode)

> **状态说明**：DeoVR 已不再支持红蓝立体模式（Anaglyph mode）。

---

## 13. 透视模式 / 混合现实 (Passthrough mode)

透视（Passthrough）是 DeoVR 将虚拟世界与真实物理环境融合的方式，亦可称为混合现实（MR）或增强现实（AR）。它利用 VR 头显上的透视/AR 摄像头功能，将 VR 内容带入用户的真实空间中。

### 支持的头显设备：
- **Quest 3 & 3S**（彩色双目立体）
- **Apple Vision Pro**（即将支持）
- **Quest Pro**（彩色双目立体）
- **Pico 4**（彩色单目）
- **Quest 2**（黑白单目）
- **Pico Neo 3 Pro**（黑白）
- **Valve Index**（黑白）

### 上传透视内容到 DeoVR
早期的 Passthrough 透视仅支持扣除单一纯色背景，例如黑底、绿底或白底。默认设置为纯黑色。创作者可以通过色相（Hue）、饱和度（Saturation）、亮度（Brightness）、色彩范围（Color Range）和边缘羽化（Falloff / Feather）等参数来微调扣像效果。

创作者可在上传视频时（或稍后通过 DeoVR 创作者后台面板）为视频配置特定的透视扣像参数。

#### 历史描述标签格式 (Legacy)
早期如果创作者想指定抠像颜色背景，可以在视频详情描述中直接写入以下配置，DeoVR 审核人员会将其设为该视频的默认值：

```text
#passthrough settings#
Hue 30
Saturation 100
Brightness 100
Color Range 360
Falloff 1000
```
或者简写为纯数值（依次为色相、饱和度、亮度、色彩范围、羽化）：
```text
#passthrough settings#
30
100
100
360
1000
```

#### 实战操作流程（以纯黑背景扣像为例）：
1. 在 DeoVR 播放界面按下扳机键呼出播放控制主菜单。
2. 在右侧面板中找到透视设置选项卡（**眼睛图标**）。
3. 点击顶部的 **“Passthrough”** 切换开关激活透视模式。
4. 默认会自动选择黑色作为透视抠除背景，视频背景随即变为透明，显示出周围真实环境。
5. 微调色彩范围（Range）和羽化（Falloff）滑块以达到理想边缘。

**颜色拾取参考：**
- **黑底抠像**：在 Adobe After Effects 或其它后期剪辑软件中，背景色需设为绝对纯黑十六进制值 `#000000`。
  > *注意：黑底抠像容易产生边缘伪影；若画面人物穿着黑色鞋子或深色衣物，这些部位也会被误扣透明。因此推荐使用绿幕或蓝幕。*
- **绿底抠像**：DeoVR 官方推荐的透视绿幕 Hex 颜色代码为 **`#2BE640`**。绿幕抠像更干净、边缘伪影明显更少。

---

## 14. 鱼眼视频 Alpha 通道透视 (13.9+) (Alpha channel passthrough for Fisheye videos)

最新且最高级的透视模式称为 **Alpha 通道透视 (Alpha Passthrough)**。它通过在视频内部嵌入一条独立的 Alpha 灰度通道来实现透明度。

虽然制作工艺略复杂，但它能提供极其精准的高画质抠像，无需在头显中进行任何复杂的色彩阈值调整，用户在播放器中点击一个按钮即可在透视与非透视之间无缝切换。目前该方案主要适用于鱼眼投影 VR 视频（利用鱼眼画面外的空白黑边区域打包 Alpha 通道）。

### 核心优势：
1. **将绿幕抠像前置于后期制作阶段**：头显端呈现的效果远超机内实时色度键（Chroma Key）抠像。例如人物的发丝、透明半透明织物等精细细节完全不会丢失。创作者在后期还可以自由进行画面调色，无需担心破坏应用内的绿幕抠像。
2. **支持任意视频转换**：结合先进的 AI 智能抠图算法，几乎可以将任何普通视频生成高精度的 Alpha 遮罩，轻松转变为透视版本。

### 打包制作指南：
在后期合成软件中，导出两份相同分辨率与帧率的视频：
- 份一：抑制了绿幕边缘溢色的常规彩色视频；
- 份二：黑白 Alpha 遮罩视频（后续只需提取其红通道）。

### 官方打包工具 (Deo-Alpha-Packer)：
1. 下载工具：[Deo-Alpha-Packer.zip](https://rest.s3for.me/insights.deovr.com/apps/Deo-Alpha-Packer.zip)
2. 解压并运行 `Deo-Alpha-Packer.exe`。
3. 在设置中指定本机安装的 `ffmpeg` 路径。
4. 分别导入彩色视频（Color）与 Alpha 遮罩视频，点击 "Export" 一键合成。

### 手动排版与预设下载：
若在专业合成软件中手动排版，请参照官方规范将 Alpha 遮罩排列在鱼眼圆球两侧空隙区域。
- **Adobe Premiere 预设包**：[ALPHA_8K_PRESET.rar](https://insights.deovr.com/alpha_premiere_preset/ALPHA_8K_PRESET.rar)（包含双遮罩、示例工程及教学视频）。
- **Adobe After Effects 预设包**：[AFTER_EFFECTS_DEO.rar](https://insights.deovr.com/alpha_aftereffects_preset/AFTER_EFFECTS_DEO.rar)（包含 AE 工程模板、示例视频及教学）。

**发布命名**：  
制作完成后，在视频文件名中加入 `_alpha` 标识（例如 `MyVideo_SBS_alpha_mkx200.mp4`），或者在 DeoVR Drive 上传时勾选 **“Has Alpha”** 选项。上传包含 `_alpha` 的视频后平台会自动识别，DeoVR 播放器将展现无瑕疵的透视画面。

---

## 15. 字幕 (Subtitles)

DeoVR 具备强大的字幕渲染系统，支持自由调整字幕位置、缩放比例、背景不透明度以及三维空间景深（Depth）。

### 格式与字符集支持：
- **字幕文件格式**：`.srt` 格式。
- **字符集语言支持**：全欧洲语言、全西里尔文、中文（Chinese）、日文（Japanese）、韩文（Korean）、亚美尼亚文、格鲁吉亚文、泰文、马拉雅拉姆文。

### 使用与配置方法：
- **DeoVR.com 在线流媒体**：
  1. 在播放主面板上开启“Subtitles”开关。
  2. 切换语言：进入播放器设置 `Player Settings > Subtitles` 标签页，在下拉列表中切换可选语言。
  3. 也可以点击“Open”（文件夹图标）加载本地自定义字幕。
- **本地视频播放**：
  - 将 `.srt` 字幕文件放置在视频所在的同一文件夹内。
  - 将字幕文件名重命名为与视频文件名完全一致（仅扩展名不同）。

---

## 16. 支持的本地文件格式与编解码器 (Supported local file formats and codecs)

DeoVR 在播放本地文件时支持目前绝大多数主流的视音频封装与编解码器。Windows 客户端与 Android / 独立一体机客户端（Meta Quest / Pico）支持情况存在细微差异：

### 1. 支持的文件封装容器：
- **Windows 客户端**：`AVI`, `MPEG`, `MP4`, `MOV`, `MPG`, `M4V`, `JPG`, `PNG`, `M3U8`
- **Quest / Pico 一体机客户端**：`MPEG`, `MP4`, `MKV`, `WebM`, `MPG`, `M4V`, `JPG`, `PNG`, `M3U8`

### 2. 视频编解码器 (Video Codecs)：
- **Windows 客户端**：  
  `HEVC/H.265`, `H.264`, `H.263 (DivX/XVid)`, `MJPEG`, `WMV`, `VP8`, `VP9`, `Hap`, `NotchLC`, `DV`
- **Quest / Pico 一体机客户端**：  
  `AV1`（Quest 3 / 3S 原生支持）, `HEVC/H.265`, `H.264`, `H.263 (DivX/XVid)`, `VP8`, `VP9`, `DV`, `Uncompressed R10K`, `Uncompressed V210`, `Uncompressed 2VUY`

### 3. 音频编解码器 (Audio Codecs)：
- **Windows 客户端**：  
  `MP3`, `AAC`, `WAV`, `FLAC`, `OPUS`, `µLAW`, `ADPCM`, `Linear PCM`
- **Quest / Pico 一体机客户端**：  
  `MP3`, `AAC`, `WAV`, `FLAC`, `OPUS`, `Linear PCM`

---

## 17. 获取 DeoVR 客户端 (Get the DeoVR App)

可以通过以下官方平台下载并体验最新版本的 DeoVR 客户端：

- [DeoVR App 官方下载聚合页](https://deovr.com/app)
- **Meta Quest 商店**：[Quest 3S / 3 / Pro / 2 官方版](https://www.oculus.com/experiences/quest/2382576078453818/)
- **SteamVR (PCVR)**：[HTC Vive Pro/Cosmos、Windows Mixed Reality](http://store.steampowered.com/app/837380/DeoVR_Video_Player/)
- **PICO 官方商店**：[Neo 3 / 3 Pro / 4](https://store-global.picoxr.com/global/detail/1/7093170485210398725?utm_source=official)
- **Sidequest 社区版**：
  - [DeoVR for Meta Quest](https://sidequestvr.com/app/1347/deovr-quest)
  - [DeoVR for Pico](https://sidequestvr.com/app/12018/deovr-pico)
- **Apple Vision Pro**：客户端即将推出，目前可参考 [Vision Pro Safari 观看指南](https://deovr.com/blog/80-vr-videos-on-the-apple-vision-pro)

> **版本提示**：为了获得最佳体验，建议搭配 Meta Quest 3S 或 3 等主流头显，并保持固件与 DeoVR 应用处于最新版本。官方对老旧版本的支持具有时效性，请及时升级。
