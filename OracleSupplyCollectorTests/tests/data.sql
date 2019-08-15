--- Test data

create table test_data_types (
   id INTEGER not null,
   char_field char(40),
   varchar_field varchar(100),
   varchar2_field varchar2(100),
   nvarchar2_field nvarchar2(100),
   number_field number,
   date_field date,
   timestamp_field timestamp,
   timestamptz_field timestamp with time zone,
   constraint test_data_type_pk PRIMARY KEY(id)
);

insert into test_data_types(id, char_field, varchar_field, varchar2_field, number_field, date_field, timestamp_field, timestamptz_field)
values(1, 'char!', 'varchar!', 'varchar2!', 6.02214076, TO_DATE('2019/08/13', 'yyyy/mm/dd'), current_timestamp, current_timestamp);

create table test_field_names (
   id integer not null,
   low_case integer,
   UPCASE integer,
   CamelCase integer,
   "Table" integer,
   "array" integer,
   "SELECT" integer,
   constraint test_field_names_pk primary key(id)
);

insert into test_field_names(id, low_case, upcase, camelcase, "Table", "array", "SELECT")
values(1,0,0,0,0,0,0);

create table test_index (
   id integer NOT NULL,
   name varchar2(100) NOT NULL,
   constraint test_index_pk primary key(id),
   constraint test_index_nameunique unique(name)
);

insert into test_index(id, name)
values(1, 'Sunday');
insert into test_index(id, name)
values(2, 'Monday');
insert into test_index(id, name)
values(3, 'Tuesday');
insert into test_index(id, name)
values(4, 'Wednesday');
insert into test_index(id, name)
values(5, 'Thursday');
insert into test_index(id, name)
values(6, 'Friday');
insert into test_index(id, name)
values(7, 'Saturday');

create index text_index_name_ind on test_index(name);

create table test_index_ref (
   id integer not null,
   index_id integer,
   constraint test_index_r FOREIGN KEY(index_id) REFERENCES test_index(id),
   constraint test_index_ref_pk primary key(id)
);

insert into test_index_ref(id, index_id)
values(1, 1);
insert into test_index_ref(id, index_id)
values(2, 5);

COMMIT;

ANALYZE TABLE test_data_types COMPUTE STATISTICS;
ANALYZE TABLE test_field_names COMPUTE STATISTICS;
ANALYZE TABLE test_index COMPUTE STATISTICS;
ANALYZE TABLE test_index_ref COMPUTE STATISTICS;

quit;
/
