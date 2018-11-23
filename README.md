# Train Calendar 

自动识别转发到[12306@xware.io](mailto:12306@xware.io)中的**12306订票通知邮件**的车票信息，并提供iCS日历文件(https://12306.xware.io/ical/YOUR_EMAIL)下载。

## 大概工作流程

- 初始化12306数据并保存到 [LiteDB 数据库](https://github.com/mbdavid/LiteDB) 中，并周期性更新
  - 12306接口 [RailsApiService](./TrainCalendar/Services/RailsApiService.cs)
  - 车站信息 [StationService](./TrainCalendar/Services/StationService.cs)
  - 车次信息 [TrainService](./TrainCalendar/Services/TrainService.cs)
- 使用 IMAP 方式登录接收邮箱(12306@xware.io)
- 使用定期检查邮箱中发件人为**12306@rails.com.cn**的邮件
- 解析邮件中的 **订票、改签、退票** 信息，并通过12306的接口获取车次的详细信息（到达时间、站台等），最后保存到数据库中。

 
## 使用方法
- 在邮箱中设置转发规则，将来源为 **12306@rails.com.cn** 的邮件转发到 [12306@xware.io](mailto:12306@xware.io)
- 用户通过 https://12306.xware.io/ical/YOUR_EMAIL?day=30&name= 获取日历文件。