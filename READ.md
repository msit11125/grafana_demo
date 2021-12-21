

[Grafana Panel Plugin Template](https://github.com/grafana/grafana-starter-panel)

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

```command
// volume 指令
$ docker volume


Commands:
  create      Create a volume
  inspect     Display detailed information on one or more volumes
  ls          List volumes
  prune       Remove all unused local volumes
  rm          Remove one or more volumes


// docker-compose down volume 清除
$ docker-compose down --volumes
```

