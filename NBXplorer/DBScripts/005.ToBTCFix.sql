﻿CREATE OR REPLACE FUNCTION to_btc(v BIGINT) RETURNS NUMERIC language SQL IMMUTABLE AS $$
	   SELECT ROUND(v::NUMERIC / 100000000, 8)
$$;