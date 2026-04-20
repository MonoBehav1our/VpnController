-- Одна начальная схема: Id — UUID (TEXT, PRIMARY KEY), ярлык в колонке Email (маппится на User.Alias), 10 client UUID в JSON.
CREATE TABLE IF NOT EXISTS Users (
    Id TEXT NOT NULL PRIMARY KEY,
    Alias TEXT NOT NULL DEFAULT '',
    ClientUuidsJson TEXT NOT NULL DEFAULT '[]'
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users (Alias) WHERE Alias <> '';
