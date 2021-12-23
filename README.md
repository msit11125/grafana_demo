#### 用 Grafana 打造監控系統

- grafana.ini
  ```
  [database]
  type = postgres
  host = postgres
  name = grafana
  user = postgres
  password = postgres

  [plugins]
  allow_loading_unsigned_plugins =felix-my-grafana-plugin

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



[Grafana Panel Plugin Template](https://github.com/grafana/grafana-starter-panel)