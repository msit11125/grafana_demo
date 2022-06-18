### 用 Grafana 打造監控系統

#### 基本設定及啟動
- grafana.ini
  ```
  [database]
  type = postgres
  host = postgres
  name = grafana
  user = postgres
  password = postgres

  [plugins]
  allow_loading_unsigned_plugins =hello-my-grafana-plugin

  [security]
  allow_embedding = true
  ```

- 安裝 postgres, influxdb, elasticsearch, dejavu, grafana
  ```command
  $ docker-compose up -d
  ```
- 卸載
  ```command
  $ docker-compose down --volumes
  ```

#### Grafana Plugin 範例程式
```bash
$ cd grafana-plugins/hello-my-grafana-plugin
```
[Grafana Panel Plugin Template](https://github.com/grafana/grafana-starter-panel)


### Docker

```bash
# stop all
docker stop $(docker ps -a -q)

# remove all container
docker rm -vf $(docker ps -a -q)
```

### Reference
https://github.com/stefanprodan/dockprom
https://www.youtube.com/watch?v=SVIGKcMJ31E&ab_channel=Thetips4you