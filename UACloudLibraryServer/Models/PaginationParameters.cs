/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace AdminShell
{
    using System;

    public class PaginationParameters
    {
        private const int MaxResultSize = 5000;

        private int _cursor;
        private int _limit;

        public PaginationParameters(string cursor, int limit)
        {
            _cursor = string.IsNullOrEmpty(cursor) || !int.TryParse(cursor, out var parsedCursor) ? 0 : parsedCursor;

            if (limit < 0)
            {
                throw new ArgumentException("Limit");
            }

            if (limit == 0)
            {
                limit = MaxResultSize;
            }

            _limit = limit;
        }

        public int Limit
        {
            get => _limit;
            set => _limit = value;
        }

        public int Cursor
        {
            get => _cursor;
            set => _cursor = value;
        }
    }
}
