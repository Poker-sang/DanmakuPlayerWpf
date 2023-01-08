# DanmakuPlayer

## 介绍

* 目前只支持 [bilibili](bilibili.com) xml格式弹幕文件的透明弹幕播放器

* 第三个版本，利用Direct2D解决了弹幕多时卡顿的问题

* UI使用WPF框架和 [wpfui](https://github.com/lepoco/wpfui) 控件库

* 获取弹幕依赖 [protobuf-net](https://github.com/protobuf-net/protobuf-net) 库

* 弹幕渲染使用 [vortice](https://github.com/amerkoleci/vortice) 的Direct2D库

* 龟速更新中

## 安装教程

下载即用

## 使用说明

⚠️：指实现比较困难的功能

### 界面

* [x] 调整透明度

* [x] 固定最上层

* [ ] ⚠️ 改变主题色

### 弹幕文件

* [x] 从本地打开

* [x] 用bilibili API通过av、BV、cid、md、ss、ep等下载

* [x] 分P获取弹幕

* [x] .xml 类型

* [x] 获取全弹幕

### 播放

* [x] 调整快进速度

* [x] 调整播放倍速

* [x] 暂停、快进等快捷键

* [x] 播放时允许调整窗口大小

* [x] 播放时调整设置

* [ ] 输入进度条

* [ ] ⚠️ 和背后播放器同步

### 弹幕

* [x] 顶端、底端、滚动、逆向、彩色弹幕

* [x] 调整透明度

* [x] 调整滚动速度

* [x] 出现位置算法优化

* [x] 弹幕不重叠

* [x] 大小弹幕

* [x] 弹幕字体

* [ ] 同屏最多（顶端、底端、滚动）弹幕限制

* [ ] 弹幕阴影、描边等效果

* [ ] ⚠️ 大小弹幕出现位置优化

* [ ] 正则屏蔽弹幕

* [ ] ⚠️ 合并类似弹幕

* [ ] ⚠️ 高级弹幕

### 其他

* [x] 弹幕多时流畅度

* [x] 优化项目结构

* [ ] ⚠️ 降低内存占用

* [ ] 其他常用功能...（没考虑到的x）

## 关于项目

项目名称：DanmakuPlayer

项目地址：[GitHub](https://github.com/Poker-sang/DanmakuPlayer)

版本：2.43

## 联系方式

作者：[扑克](https://github.com/Poker-sang)

邮箱：poker_sang@outlook.com

QQ：[2639914082](http://wpa.qq.com/msgrd?v=3&uin=2639914082&site=qq&menu=yes)

2022.11.22
